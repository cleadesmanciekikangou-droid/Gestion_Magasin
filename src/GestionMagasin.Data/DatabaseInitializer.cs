using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

/// <summary>Applique les migrations SQLite dans l'ordre et refuse une base plus récente.</summary>
public sealed class DatabaseInitializer(SqliteConnectionFactory connectionFactory)
{
    private const int CurrentVersion = 3;

    public void Initialize()
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using (var foreignKeys = connection.CreateCommand())
        {
            foreignKeys.CommandText = "PRAGMA foreign_keys = ON;";
            foreignKeys.ExecuteNonQuery();
        }
        using var versionCommand = connection.CreateCommand(); versionCommand.CommandText = "PRAGMA user_version;";
        var version = Convert.ToInt32(versionCommand.ExecuteScalar());
        if (version > CurrentVersion) throw new InvalidOperationException($"La base (version {version}) est plus récente que cette application.");
        if (version < 1) ApplyVersion1(connection);
        if (version < 2) ApplyVersion2(connection);
        if (version < 3) ApplyVersion3(connection);
    }

    private static void ApplyVersion1(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction(); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE VersionSchema (Version INTEGER NOT NULL PRIMARY KEY, AppliqueLe TEXT NOT NULL);
            CREATE TABLE Parametre (Cle TEXT NOT NULL PRIMARY KEY, Valeur TEXT NOT NULL, ModifieLe TEXT NOT NULL);
            CREATE TABLE Utilisateur (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Nom TEXT NOT NULL, Identifiant TEXT NOT NULL COLLATE NOCASE UNIQUE, MotDePasseHash TEXT NOT NULL, Sel TEXT NOT NULL, ParametresHachage TEXT NOT NULL, Role TEXT NOT NULL, Actif INTEGER NOT NULL DEFAULT 1 CHECK (Actif IN (0, 1)));
            CREATE TABLE Categorie (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Nom TEXT NOT NULL COLLATE NOCASE UNIQUE);
            CREATE TABLE Produit (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Reference TEXT NOT NULL COLLATE NOCASE UNIQUE, Nom TEXT NOT NULL, CategorieId INTEGER NOT NULL, PrixVente NUMERIC NOT NULL CHECK (PrixVente >= 0), SeuilAlerte INTEGER NOT NULL DEFAULT 0 CHECK (SeuilAlerte >= 0), Actif INTEGER NOT NULL DEFAULT 1 CHECK (Actif IN (0, 1)), DateCreation TEXT NOT NULL, FOREIGN KEY (CategorieId) REFERENCES Categorie(Id) ON UPDATE CASCADE ON DELETE RESTRICT);
            CREATE INDEX IX_Produit_CategorieId ON Produit(CategorieId);
            INSERT INTO VersionSchema (Version, AppliqueLe) VALUES (1, $appliedAt);
            PRAGMA user_version = 1;
            """;
        command.Parameters.AddWithValue("$appliedAt", DateTimeOffset.UtcNow.ToString("O")); command.ExecuteNonQuery(); transaction.Commit();
    }

    private static void ApplyVersion2(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction(); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE MouvementStock (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ProduitId INTEGER NOT NULL,
                Type TEXT NOT NULL CHECK (Type IN ('StockInitial','Reapprovisionnement','VenteValidee','AnnulationVente','CorrectionInventaire')),
                Quantite INTEGER NOT NULL CHECK (Quantite <> 0), Motif TEXT NOT NULL, DateMouvement TEXT NOT NULL,
                MouvementOrigineId INTEGER NULL,
                FOREIGN KEY (ProduitId) REFERENCES Produit(Id) ON UPDATE CASCADE ON DELETE RESTRICT,
                FOREIGN KEY (MouvementOrigineId) REFERENCES MouvementStock(Id) ON DELETE RESTRICT,
                CHECK ((Type IN ('StockInitial','Reapprovisionnement','AnnulationVente') AND Quantite > 0) OR (Type = 'VenteValidee' AND Quantite < 0) OR Type = 'CorrectionInventaire'),
                CHECK ((Type = 'AnnulationVente' AND MouvementOrigineId IS NOT NULL) OR (Type <> 'AnnulationVente' AND MouvementOrigineId IS NULL))
            );
            CREATE UNIQUE INDEX UX_MouvementStock_AnnulationOrigine ON MouvementStock(MouvementOrigineId) WHERE MouvementOrigineId IS NOT NULL;
            CREATE INDEX IX_MouvementStock_ProduitId_Date ON MouvementStock(ProduitId, DateMouvement);
            INSERT INTO VersionSchema (Version, AppliqueLe) VALUES (2, $appliedAt);
            PRAGMA user_version = 2;
            """;
        command.Parameters.AddWithValue("$appliedAt", DateTimeOffset.UtcNow.ToString("O")); command.ExecuteNonQuery(); transaction.Commit();
    }

    private static void ApplyVersion3(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction(); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE Vente (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                JetonValidation TEXT NOT NULL UNIQUE,
                DateVente TEXT NOT NULL,
                ModePaiement TEXT NOT NULL CHECK (ModePaiement IN ('Especes','MobileMoney','Carte')),
                Total NUMERIC NOT NULL CHECK (Total >= 0),
                MontantRecu NUMERIC NOT NULL CHECK (MontantRecu >= Total),
                MonnaieRendue NUMERIC NOT NULL CHECK (MonnaieRendue >= 0)
            );
            CREATE TABLE LigneVente (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                VenteId INTEGER NOT NULL,
                ProduitId INTEGER NOT NULL,
                ReferenceProduitSnapshot TEXT NOT NULL,
                NomProduitSnapshot TEXT NOT NULL,
                PrixUnitaireSnapshot NUMERIC NOT NULL CHECK (PrixUnitaireSnapshot >= 0),
                Quantite INTEGER NOT NULL CHECK (Quantite > 0),
                TotalLigne NUMERIC NOT NULL CHECK (TotalLigne >= 0),
                FOREIGN KEY (VenteId) REFERENCES Vente(Id) ON DELETE RESTRICT,
                FOREIGN KEY (ProduitId) REFERENCES Produit(Id) ON DELETE RESTRICT,
                UNIQUE (VenteId, ProduitId)
            );
            ALTER TABLE MouvementStock ADD COLUMN VenteId INTEGER NULL REFERENCES Vente(Id) ON DELETE RESTRICT;
            CREATE INDEX IX_LigneVente_VenteId ON LigneVente(VenteId);
            CREATE INDEX IX_MouvementStock_VenteId ON MouvementStock(VenteId);
            INSERT INTO VersionSchema (Version, AppliqueLe) VALUES (3, $appliedAt);
            PRAGMA user_version = 3;
            """;
        command.Parameters.AddWithValue("$appliedAt", DateTimeOffset.UtcNow.ToString("O")); command.ExecuteNonQuery(); transaction.Commit();
    }
}
