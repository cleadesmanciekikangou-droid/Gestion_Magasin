using System.Globalization;
using GestionMagasin.Models;
using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

/// <summary>Valide une vente, ses lignes et les sorties de stock dans une transaction SQLite unique.</summary>
public sealed class SqliteVenteRepository(SqliteConnectionFactory factory) : IVenteRepository
{
    public VenteValidee ValiderVente(string jetonValidation, IReadOnlyList<LigneVenteDemandee> lignes, ModePaiement modePaiement, decimal montantRecu)
    {
        if (string.IsNullOrWhiteSpace(jetonValidation) || jetonValidation.Length > 80)
            throw new ArgumentException("Jeton de validation invalide.", nameof(jetonValidation));
        if (lignes.Count == 0) throw new ArgumentException("Le panier est vide.", nameof(lignes));
        if (lignes.Any(l => l.ProduitId <= 0 || l.Quantite <= 0 || l.PrixUnitaireAttendu < 0))
            throw new ArgumentException("Le panier contient une ligne invalide.", nameof(lignes));
        if (lignes.Select(l => l.ProduitId).Distinct().Count() != lignes.Count)
            throw new ArgumentException("Un produit ne peut apparaître qu'une fois dans le panier.", nameof(lignes));

        using var connection = factory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);

        // Un même jeton ne peut créer qu'une vente. Une relance réseau retourne la vente initiale.
        using (var existing = connection.CreateCommand())
        {
            existing.Transaction = transaction;
            existing.CommandText = "SELECT Id, DateVente, ModePaiement, Total, MontantRecu, MonnaieRendue FROM Vente WHERE JetonValidation=$token;";
            existing.Parameters.AddWithValue("$token", jetonValidation);
            using var reader = existing.ExecuteReader();
            if (reader.Read())
            {
                var result = ReadSale(reader, jetonValidation);
                reader.Close();
                transaction.Commit();
                return result;
            }
        }

        var checkedLines = new List<ValidatedLine>(lignes.Count);
        decimal total = 0;
        foreach (var line in lignes)
        {
            using var check = connection.CreateCommand();
            check.Transaction = transaction;
            check.CommandText = """
                SELECT p.Reference, p.Nom, p.PrixVente, p.Actif,
                       COALESCE((SELECT SUM(m.Quantite) FROM MouvementStock m WHERE m.ProduitId=p.Id), 0)
                FROM Produit p WHERE p.Id=$product;
                """;
            check.Parameters.AddWithValue("$product", line.ProduitId);
            using var reader = check.ExecuteReader();
            if (!reader.Read()) throw new InvalidOperationException($"Le produit {line.ProduitId} n'existe plus.");
            var reference = reader.GetString(0);
            var name = reader.GetString(1);
            var currentPrice = Convert.ToDecimal(reader.GetValue(2), CultureInfo.InvariantCulture);
            var active = reader.GetInt64(3) == 1;
            var stock = reader.GetInt64(4);
            reader.Close();

            if (!active) throw new InvalidOperationException($"Le produit « {name} » est archivé et ne peut pas être vendu.");
            if (currentPrice != line.PrixUnitaireAttendu)
                throw new InvalidOperationException($"Le prix du produit « {name} » a changé. Actualisez le panier avant de valider.");
            if (stock < 0 || line.Quantite > stock)
                throw new InvalidOperationException($"Stock insuffisant pour « {name} » : {Math.Max(0, stock)} disponible(s), {line.Quantite} demandé(s).");

            var lineTotal = checked(currentPrice * line.Quantite);
            total = checked(total + lineTotal);
            checkedLines.Add(new ValidatedLine(line.ProduitId, reference, name, currentPrice, line.Quantite, lineTotal));
        }

        decimal change;
        if (modePaiement == ModePaiement.Especes)
        {
            if (montantRecu < total) throw new InvalidOperationException($"Montant reçu insuffisant. Total à payer : {total:0.00}.");
            change = montantRecu - total;
        }
        else
        {
            if (montantRecu != total) throw new InvalidOperationException("Pour ce mode de paiement, le montant reçu doit correspondre exactement au total.");
            change = 0;
        }

        var now = DateTimeOffset.UtcNow;
        using (var sale = connection.CreateCommand())
        {
            sale.Transaction = transaction;
            sale.CommandText = """
                INSERT INTO Vente (JetonValidation, DateVente, ModePaiement, Total, MontantRecu, MonnaieRendue)
                VALUES ($token, $date, $payment, $total, $received, $change);
                SELECT last_insert_rowid();
                """;
            sale.Parameters.AddWithValue("$token", jetonValidation);
            sale.Parameters.AddWithValue("$date", now.ToString("O"));
            sale.Parameters.AddWithValue("$payment", modePaiement.ToString());
            sale.Parameters.AddWithValue("$total", total);
            sale.Parameters.AddWithValue("$received", montantRecu);
            sale.Parameters.AddWithValue("$change", change);
            var saleId = Convert.ToInt64(sale.ExecuteScalar(), CultureInfo.InvariantCulture);

            foreach (var line in checkedLines)
            {
                using var insertLine = connection.CreateCommand();
                insertLine.Transaction = transaction;
                insertLine.CommandText = """
                    INSERT INTO LigneVente (VenteId, ProduitId, ReferenceProduitSnapshot, NomProduitSnapshot, PrixUnitaireSnapshot, Quantite, TotalLigne)
                    VALUES ($sale, $product, $reference, $name, $price, $quantity, $lineTotal);
                    """;
                insertLine.Parameters.AddWithValue("$sale", saleId);
                insertLine.Parameters.AddWithValue("$product", line.ProduitId);
                insertLine.Parameters.AddWithValue("$reference", line.Reference);
                insertLine.Parameters.AddWithValue("$name", line.Name);
                insertLine.Parameters.AddWithValue("$price", line.UnitPrice);
                insertLine.Parameters.AddWithValue("$quantity", line.Quantity);
                insertLine.Parameters.AddWithValue("$lineTotal", line.Total);
                insertLine.ExecuteNonQuery();

                using var movement = connection.CreateCommand();
                movement.Transaction = transaction;
                movement.CommandText = """
                    INSERT INTO MouvementStock (ProduitId, Type, Quantite, Motif, DateMouvement, MouvementOrigineId, VenteId)
                    VALUES ($product, 'VenteValidee', $quantity, $reason, $date, NULL, $sale);
                    """;
                movement.Parameters.AddWithValue("$product", line.ProductId);
                movement.Parameters.AddWithValue("$quantity", -line.Quantity);
                movement.Parameters.AddWithValue("$reason", $"Vente #{saleId}");
                movement.Parameters.AddWithValue("$date", now.ToString("O"));
                movement.Parameters.AddWithValue("$sale", saleId);
                movement.ExecuteNonQuery();
            }

            transaction.Commit();
            return new VenteValidee(saleId, jetonValidation, now, modePaiement, total, montantRecu, change);
        }
    }

    private static VenteValidee ReadSale(SqliteDataReader reader, string token) => new(
        reader.GetInt64(0), token, DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
        Enum.Parse<ModePaiement>(reader.GetString(2)), Convert.ToDecimal(reader.GetValue(3), CultureInfo.InvariantCulture),
        Convert.ToDecimal(reader.GetValue(4), CultureInfo.InvariantCulture), Convert.ToDecimal(reader.GetValue(5), CultureInfo.InvariantCulture));

    private sealed record ValidatedLine(long ProductId, string Reference, string Name, decimal UnitPrice, long Quantity, decimal Total);
}
