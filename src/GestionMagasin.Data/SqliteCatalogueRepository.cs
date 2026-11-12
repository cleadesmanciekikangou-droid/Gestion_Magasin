using GestionMagasin.Models;
using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

/// <summary>Catalogue et stock calculé exclusivement à partir des mouvements.</summary>
public sealed class SqliteCatalogueRepository(SqliteConnectionFactory factory) : ICatalogueRepository
{
    public IReadOnlyList<Produit> RechercherProduits(string? recherche = null, long? categorieId = null, bool? actif = null)
    {
        using var connection = factory.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.Id, p.Reference, p.Nom, p.CategorieId, c.Nom, p.PrixVente, p.SeuilAlerte,
                   p.Actif, p.DateCreation, COALESCE(SUM(m.Quantite), 0) AS Stock
            FROM Produit p
            JOIN Categorie c ON c.Id = p.CategorieId
            LEFT JOIN MouvementStock m ON m.ProduitId = p.Id
            WHERE ($search IS NULL OR p.Reference LIKE $pattern ESCAPE '\\' OR p.Nom LIKE $pattern ESCAPE '\\')
              AND ($category IS NULL OR p.CategorieId = $category)
              AND ($active IS NULL OR p.Actif = $active)
            GROUP BY p.Id
            ORDER BY p.Nom COLLATE NOCASE;
            """;
        var normalized = string.IsNullOrWhiteSpace(recherche) ? null : recherche.Trim();
        command.Parameters.AddWithValue("$search", (object?)normalized ?? DBNull.Value);
        command.Parameters.AddWithValue("$pattern", (object?)(normalized is null ? null : $"%{normalized.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%") ?? DBNull.Value);
        command.Parameters.AddWithValue("$category", (object?)categorieId ?? DBNull.Value);
        command.Parameters.AddWithValue("$active", actif.HasValue ? (actif.Value ? 1 : 0) : DBNull.Value);
        using var reader = command.ExecuteReader();
        var results = new List<Produit>();
        while (reader.Read())
            results.Add(new Produit
            {
                Id = reader.GetInt64(0), Reference = reader.GetString(1), Nom = reader.GetString(2),
                CategorieId = reader.GetInt64(3), CategorieNom = reader.GetString(4),
                PrixVente = reader.GetDecimal(5), SeuilAlerte = reader.GetInt32(6),
                Actif = reader.GetInt64(7) == 1, DateCreation = DateTimeOffset.Parse(reader.GetString(8)),
                Stock = reader.GetInt64(9)
            });
        return results;
    }

    public IReadOnlyList<Categorie> ObtenirCategories()
    {
        using var connection = factory.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Nom FROM Categorie ORDER BY Nom COLLATE NOCASE;";
        using var reader = command.ExecuteReader();
        var results = new List<Categorie>();
        while (reader.Read()) results.Add(new Categorie(reader.GetInt64(0), reader.GetString(1)));
        return results;
    }

    public long CreerCategorie(string nom)
    {
        nom = nom.Trim();
        if (nom.Length is < 2 or > 100) throw new ArgumentException("Le nom de catégorie doit contenir 2 à 100 caractères.");
        using var connection = factory.CreateConnection(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Categorie (Nom) VALUES ($name); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$name", nom);
        try { return (long)command.ExecuteScalar()!; }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { throw new InvalidOperationException("Cette catégorie existe déjà.", ex); }
    }

    public long CreerProduit(string reference, string nom, long categorieId, decimal prixVente, int seuilAlerte)
    {
        Validate(reference, nom, categorieId, prixVente, seuilAlerte);
        using var connection = factory.CreateConnection(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Produit (Reference, Nom, CategorieId, PrixVente, SeuilAlerte, Actif, DateCreation)
            VALUES ($reference, $name, $category, $price, $threshold, 1, $created);
            SELECT last_insert_rowid();
            """;
        AddProductParameters(command, reference, nom, categorieId, prixVente, seuilAlerte);
        command.Parameters.AddWithValue("$created", DateTimeOffset.UtcNow.ToString("O"));
        try { return (long)command.ExecuteScalar()!; }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { throw new InvalidOperationException("Référence déjà utilisée ou catégorie invalide.", ex); }
    }

    public void ModifierProduit(long id, string reference, string nom, long categorieId, decimal prixVente, int seuilAlerte)
    {
        Validate(reference, nom, categorieId, prixVente, seuilAlerte);
        using var connection = factory.CreateConnection(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Produit SET Reference=$reference, Nom=$name, CategorieId=$category, PrixVente=$price, SeuilAlerte=$threshold WHERE Id=$id;";
        AddProductParameters(command, reference, nom, categorieId, prixVente, seuilAlerte);
        command.Parameters.AddWithValue("$id", id);
        try { if (command.ExecuteNonQuery() != 1) throw new KeyNotFoundException("Produit introuvable."); }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { throw new InvalidOperationException("Référence déjà utilisée ou catégorie invalide.", ex); }
    }

    public void DefinirProduitActif(long id, bool actif)
    {
        using var connection = factory.CreateConnection(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Produit SET Actif=$active WHERE Id=$id;";
        command.Parameters.AddWithValue("$active", actif ? 1 : 0); command.Parameters.AddWithValue("$id", id);
        if (command.ExecuteNonQuery() != 1) throw new KeyNotFoundException("Produit introuvable.");
    }

    private static void Validate(string reference, string nom, long category, decimal price, int threshold)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Trim().Length > 80) throw new ArgumentException("La référence est obligatoire (80 caractères maximum).");
        if (string.IsNullOrWhiteSpace(nom) || nom.Trim().Length > 200) throw new ArgumentException("Le nom est obligatoire (200 caractères maximum).");
        if (category <= 0 || price < 0 || threshold < 0) throw new ArgumentException("Catégorie, prix ou seuil invalide.");
    }

    private static void AddProductParameters(SqliteCommand command, string reference, string nom, long category, decimal price, int threshold)
    {
        command.Parameters.AddWithValue("$reference", reference.Trim()); command.Parameters.AddWithValue("$name", nom.Trim());
        command.Parameters.AddWithValue("$category", category); command.Parameters.AddWithValue("$price", price);
        command.Parameters.AddWithValue("$threshold", threshold);
    }
}
