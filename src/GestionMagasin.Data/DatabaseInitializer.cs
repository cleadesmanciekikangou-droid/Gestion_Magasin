using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

/// <summary>Applique les migrations SQLite dans l'ordre et refuse une base plus récente.</summary>
public sealed class DatabaseInitializer(SqliteConnectionFactory connectionFactory)
{
    private const int CurrentVersion = 1;

    public void Initialize()
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        // Réaffirmé explicitement sur chaque connexion, en plus de l'option de connexion.
        using (var foreignKeys = connection.CreateCommand())
        {
            foreignKeys.CommandText = "PRAGMA foreign_keys = ON;";
            foreignKeys.ExecuteNonQuery();
        }

        using var versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "PRAGMA user_version;";
        var version = Convert.ToInt32(versionCommand.ExecuteScalar());
        if (version > CurrentVersion)
            throw new InvalidOperationException($"La base (version {version}) est plus récente que cette application.");

        if (version < 1)
            ApplyVersion1(connection);
    }

    private static void ApplyVersion1(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE VersionSchema (
                Version INTEGER NOT NULL PRIMARY KEY,
                AppliqueLe TEXT NOT NULL
            );

            CREATE TABLE Parametre (
                Cle TEXT NOT NULL PRIMARY KEY,
                Valeur TEXT NOT NULL,
                ModifieLe TEXT NOT NULL
            );

            CREATE TABLE Utilisateur (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Nom TEXT NOT NULL,
                Identifiant TEXT NOT NULL COLLATE NOCASE UNIQUE,
                MotDePasseHash TEXT NOT NULL,
                Sel TEXT NOT NULL,
                ParametresHachage TEXT NOT NULL,
                Role TEXT NOT NULL,
                Actif INTEGER NOT NULL DEFAULT 1 CHECK (Actif IN (0, 1))
            );

            CREATE TABLE Categorie (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Nom TEXT NOT NULL COLLATE NOCASE UNIQUE
            );

            CREATE TABLE Produit (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Reference TEXT NOT NULL COLLATE NOCASE UNIQUE,
                Nom TEXT NOT NULL,
                CategorieId INTEGER NOT NULL,
                PrixVente NUMERIC NOT NULL CHECK (PrixVente >= 0),
                SeuilAlerte INTEGER NOT NULL DEFAULT 0 CHECK (SeuilAlerte >= 0),
                Actif INTEGER NOT NULL DEFAULT 1 CHECK (Actif IN (0, 1)),
                DateCreation TEXT NOT NULL,
                FOREIGN KEY (CategorieId) REFERENCES Categorie(Id) ON UPDATE CASCADE ON DELETE RESTRICT
            );

            CREATE INDEX IX_Produit_CategorieId ON Produit(CategorieId);
            INSERT INTO VersionSchema (Version, AppliqueLe) VALUES (1, $appliedAt);
            PRAGMA user_version = 1;
            """;
        command.Parameters.AddWithValue("$appliedAt", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
        transaction.Commit();
    }
}
