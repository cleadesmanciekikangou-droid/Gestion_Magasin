using GestionMagasin.Models;
using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

/// <summary>Écrit les mouvements dans des transactions sérialisées et interdit tout stock négatif.</summary>
public sealed class SqliteStockRepository(SqliteConnectionFactory factory) : IStockRepository
{
    public long ObtenirStock(long produitId)
    {
        using var connection = factory.CreateConnection(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(SUM(Quantite), 0) FROM MouvementStock WHERE ProduitId=$product;";
        command.Parameters.AddWithValue("$product", produitId);
        return Convert.ToInt64(command.ExecuteScalar());
    }

    public IReadOnlyList<MouvementStock> ObtenirMouvements(long? produitId = null, int limite = 500)
    {
        limite = Math.Clamp(limite, 1, 5000);
        using var connection = factory.CreateConnection(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT m.Id, m.ProduitId, p.Reference, p.Nom, m.Type, m.Quantite, m.Motif, m.DateMouvement, m.MouvementOrigineId
            FROM MouvementStock m JOIN Produit p ON p.Id=m.ProduitId
            WHERE ($product IS NULL OR m.ProduitId=$product)
            ORDER BY m.Id DESC LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$product", (object?)produitId ?? DBNull.Value);
        command.Parameters.AddWithValue("$limit", limite);
        using var reader = command.ExecuteReader();
        var results = new List<MouvementStock>();
        while (reader.Read()) results.Add(new MouvementStock(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2), reader.GetString(3),
            Enum.Parse<TypeMouvementStock>(reader.GetString(4)), reader.GetInt64(5), reader.GetString(6), DateTimeOffset.Parse(reader.GetString(7)),
            reader.IsDBNull(8) ? null : reader.GetInt64(8)));
        return results;
    }

    public long AjouterStockInitial(long produitId, long quantite, string motif) => AddPositive(produitId, quantite, motif, TypeMouvementStock.StockInitial);
    public long Reapprovisionner(long produitId, long quantite, string motif) => AddPositive(produitId, quantite, motif, TypeMouvementStock.Reapprovisionnement);

    public long EnregistrerVenteValidee(long produitId, long quantite, string referenceVente)
    {
        if (quantite <= 0) throw new ArgumentOutOfRangeException(nameof(quantite), "La quantité vendue doit être positive.");
        if (string.IsNullOrWhiteSpace(referenceVente)) throw new ArgumentException("La référence de vente est obligatoire.");
        using var connection = factory.CreateConnection(); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        EnsureProductCanBeSold(connection, transaction, produitId);
        EnsureSufficientStock(connection, transaction, produitId, quantite);
        var id = Insert(connection, transaction, produitId, TypeMouvementStock.VenteValidee, -quantite, $"Vente {referenceVente.Trim()}", null);
        transaction.Commit(); return id;
    }

    public long AnnulerVente(long mouvementVenteId, string motif)
    {
        ValidateReason(motif);
        using var connection = factory.CreateConnection(); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        using var find = connection.CreateCommand(); find.Transaction = transaction;
        find.CommandText = "SELECT ProduitId, Quantite, Type FROM MouvementStock WHERE Id=$id;";
        find.Parameters.AddWithValue("$id", mouvementVenteId);
        using var reader = find.ExecuteReader();
        if (!reader.Read() || reader.GetString(2) != nameof(TypeMouvementStock.VenteValidee)) throw new InvalidOperationException("Mouvement de vente introuvable.");
        var productId = reader.GetInt64(0); var soldQuantity = reader.GetInt64(1); reader.Close();
        using (var check = connection.CreateCommand())
        {
            check.Transaction = transaction; check.CommandText = "SELECT EXISTS(SELECT 1 FROM MouvementStock WHERE MouvementOrigineId=$id);";
            check.Parameters.AddWithValue("$id", mouvementVenteId);
            if (Convert.ToInt64(check.ExecuteScalar()) == 1) throw new InvalidOperationException("Cette vente a déjà été annulée.");
        }
        var id = Insert(connection, transaction, productId, TypeMouvementStock.AnnulationVente, -soldQuantity, motif.Trim(), mouvementVenteId);
        transaction.Commit(); return id;
    }

    public long CorrigerInventaire(long produitId, long quantiteComptee, string motif)
    {
        if (quantiteComptee < 0) throw new ArgumentOutOfRangeException(nameof(quantiteComptee), "Le stock compté ne peut pas être négatif.");
        ValidateReason(motif);
        using var connection = factory.CreateConnection(); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        EnsureProductExists(connection, transaction, produitId);
        var current = GetStock(connection, transaction, produitId);
        var variance = checked(quantiteComptee - current);
        if (variance == 0) throw new InvalidOperationException("Le stock compté correspond déjà au stock théorique.");
        var id = Insert(connection, transaction, produitId, TypeMouvementStock.CorrectionInventaire, variance, motif.Trim(), null);
        transaction.Commit(); return id;
    }

    private long AddPositive(long productId, long quantity, string reason, TypeMouvementStock type)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "La quantité doit être positive.");
        ValidateReason(reason);
        using var connection = factory.CreateConnection(); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        EnsureProductExists(connection, transaction, productId);
        var id = Insert(connection, transaction, productId, type, quantity, reason.Trim(), null);
        transaction.Commit(); return id;
    }

    private static long Insert(SqliteConnection connection, SqliteTransaction transaction, long productId, TypeMouvementStock type, long quantity, string reason, long? origin)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "INSERT INTO MouvementStock (ProduitId, Type, Quantite, Motif, DateMouvement, MouvementOrigineId) VALUES ($product,$type,$quantity,$reason,$date,$origin); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$product", productId); command.Parameters.AddWithValue("$type", type.ToString());
        command.Parameters.AddWithValue("$quantity", quantity); command.Parameters.AddWithValue("$reason", reason);
        command.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O")); command.Parameters.AddWithValue("$origin", (object?)origin ?? DBNull.Value);
        return (long)command.ExecuteScalar()!;
    }

    private static void EnsureProductCanBeSold(SqliteConnection connection, SqliteTransaction transaction, long productId)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT Actif FROM Produit WHERE Id=$id;"; command.Parameters.AddWithValue("$id", productId);
        var result = command.ExecuteScalar();
        if (result is null) throw new KeyNotFoundException("Produit introuvable.");
        if (Convert.ToInt64(result) != 1) throw new InvalidOperationException("Un produit archivé ne peut pas être vendu.");
    }

    private static void EnsureProductExists(SqliteConnection connection, SqliteTransaction transaction, long productId)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM Produit WHERE Id=$id);"; command.Parameters.AddWithValue("$id", productId);
        if (Convert.ToInt64(command.ExecuteScalar()) != 1) throw new KeyNotFoundException("Produit introuvable.");
    }

    private static void EnsureSufficientStock(SqliteConnection connection, SqliteTransaction transaction, long productId, long quantity)
    {
        var stock = GetStock(connection, transaction, productId);
        if (quantity > stock) throw new InvalidOperationException($"Stock insuffisant : {stock} disponible(s), {quantity} demandé(s).");
    }

    private static long GetStock(SqliteConnection connection, SqliteTransaction transaction, long productId)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT COALESCE(SUM(Quantite),0) FROM MouvementStock WHERE ProduitId=$product;";
        command.Parameters.AddWithValue("$product", productId); return Convert.ToInt64(command.ExecuteScalar());
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 300) throw new ArgumentException("Un motif de 1 à 300 caractères est requis.");
    }
}
