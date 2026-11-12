using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

public sealed class DatabaseInitializer(SqliteConnectionFactory connectionFactory)
{
    public const int SchemaVersion = 5;
    public void Initialize()
    {
        using var c = connectionFactory.CreateConnection(); c.Open();
        using (var fk = c.CreateCommand()) { fk.CommandText = "PRAGMA foreign_keys=ON;"; fk.ExecuteNonQuery(); }
        using var ver = c.CreateCommand(); ver.CommandText = "PRAGMA user_version;"; var version = Convert.ToInt32(ver.ExecuteScalar());
        if (version > SchemaVersion) throw new InvalidOperationException($"Base plus récente ({version}) que l'application.");
        if (version == 0) { using var check = c.CreateCommand(); check.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Utilisateur';"; if (Convert.ToInt32(check.ExecuteScalar()) != 0) throw new InvalidOperationException("Base sans version de schéma reconnue; sauvegardez-la avant intervention."); ApplyVersion1(c); version = 1; }
        if (version < 2) { ApplyVersion2(c); version = 2; }
        if (version < 3) { ApplyVersion3(c); version = 3; }
        if (version < 4) { ApplyVersion4(c); version = 4; }
        if (version < 5) ApplyVersion5(c);
    }
    private static void ApplyVersion1(SqliteConnection c)
    {
        using var tx = c.BeginTransaction(); using var cmd = c.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = """
            CREATE TABLE VersionSchema(Version INTEGER PRIMARY KEY,AppliqueLe TEXT NOT NULL);
            CREATE TABLE Parametre(Cle TEXT PRIMARY KEY,Valeur TEXT NOT NULL,ModifieLe TEXT NOT NULL);
            CREATE TABLE Utilisateur(Id INTEGER PRIMARY KEY AUTOINCREMENT,Nom TEXT NOT NULL,Identifiant TEXT NOT NULL COLLATE NOCASE UNIQUE,MotDePasseHash TEXT NOT NULL,Sel TEXT NOT NULL,ParametresHachage TEXT NOT NULL,Role TEXT NOT NULL,Actif INTEGER NOT NULL DEFAULT 1 CHECK(Actif IN (0,1)));
            CREATE TABLE Categorie(Id INTEGER PRIMARY KEY AUTOINCREMENT,Nom TEXT NOT NULL COLLATE NOCASE UNIQUE);
            CREATE TABLE Produit(Id INTEGER PRIMARY KEY AUTOINCREMENT,Reference TEXT NOT NULL COLLATE NOCASE UNIQUE,Nom TEXT NOT NULL,CategorieId INTEGER NOT NULL,PrixVente NUMERIC NOT NULL CHECK(PrixVente>=0),SeuilAlerte INTEGER NOT NULL DEFAULT 0 CHECK(SeuilAlerte>=0),Actif INTEGER NOT NULL DEFAULT 1 CHECK(Actif IN (0,1)),DateCreation TEXT NOT NULL,FOREIGN KEY(CategorieId) REFERENCES Categorie(Id) ON UPDATE CASCADE ON DELETE RESTRICT);
            CREATE INDEX IX_Produit_CategorieId ON Produit(CategorieId); INSERT INTO VersionSchema VALUES(1,$date); PRAGMA user_version=1;
            """; cmd.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O")); cmd.ExecuteNonQuery(); tx.Commit();
    }
    private static void ApplyVersion2(SqliteConnection c)
    {
        using var tx = c.BeginTransaction(); using var cmd = c.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = """
            CREATE TABLE MouvementStock(Id INTEGER PRIMARY KEY AUTOINCREMENT,ProduitId INTEGER NOT NULL,Type TEXT NOT NULL CHECK(Type IN ('StockInitial','Reapprovisionnement','VenteValidee','AnnulationVente','CorrectionInventaire')),Quantite INTEGER NOT NULL CHECK(Quantite<>0),Motif TEXT NOT NULL,DateMouvement TEXT NOT NULL,MouvementOrigineId INTEGER NULL,FOREIGN KEY(ProduitId) REFERENCES Produit(Id) ON DELETE RESTRICT,FOREIGN KEY(MouvementOrigineId) REFERENCES MouvementStock(Id) ON DELETE RESTRICT,CHECK((Type IN ('StockInitial','Reapprovisionnement','AnnulationVente') AND Quantite>0) OR (Type='VenteValidee' AND Quantite<0) OR Type='CorrectionInventaire'),CHECK((Type='AnnulationVente' AND MouvementOrigineId IS NOT NULL) OR (Type<>'AnnulationVente' AND MouvementOrigineId IS NULL)));
            CREATE UNIQUE INDEX UX_MouvementStock_AnnulationOrigine ON MouvementStock(MouvementOrigineId) WHERE MouvementOrigineId IS NOT NULL; CREATE INDEX IX_MouvementStock_ProduitId_Date ON MouvementStock(ProduitId,DateMouvement); INSERT INTO VersionSchema VALUES(2,$date); PRAGMA user_version=2;
            """; cmd.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O")); cmd.ExecuteNonQuery(); tx.Commit();
    }
    private static void ApplyVersion3(SqliteConnection c)
    {
        using var tx = c.BeginTransaction(); using var cmd = c.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = """
            CREATE TABLE Vente(Id INTEGER PRIMARY KEY AUTOINCREMENT,JetonValidation TEXT NOT NULL UNIQUE,DateVente TEXT NOT NULL,ModePaiement TEXT NOT NULL,Total NUMERIC NOT NULL,MontantRecu NUMERIC NOT NULL,MonnaieRendue NUMERIC NOT NULL);
            CREATE TABLE LigneVente(Id INTEGER PRIMARY KEY AUTOINCREMENT,VenteId INTEGER NOT NULL,ProduitId INTEGER NOT NULL,ReferenceProduitSnapshot TEXT NOT NULL,NomProduitSnapshot TEXT NOT NULL,PrixUnitaireSnapshot NUMERIC NOT NULL,Quantite INTEGER NOT NULL,TotalLigne NUMERIC NOT NULL,FOREIGN KEY(VenteId) REFERENCES Vente(Id) ON DELETE RESTRICT,FOREIGN KEY(ProduitId) REFERENCES Produit(Id) ON DELETE RESTRICT,UNIQUE(VenteId,ProduitId));
            ALTER TABLE MouvementStock ADD COLUMN VenteId INTEGER NULL REFERENCES Vente(Id) ON DELETE RESTRICT; CREATE INDEX IX_LigneVente_VenteId ON LigneVente(VenteId); CREATE INDEX IX_MouvementStock_VenteId ON MouvementStock(VenteId); INSERT INTO VersionSchema VALUES(3,$date); PRAGMA user_version=3;
            """; cmd.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O")); cmd.ExecuteNonQuery(); tx.Commit();
    }
    private static void ApplyVersion4(SqliteConnection c)
    {
        using var tx = c.BeginTransaction(); using var cmd = c.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = "ALTER TABLE Vente ADD COLUMN VendeurId INTEGER NULL REFERENCES Utilisateur(Id); ALTER TABLE Vente ADD COLUMN Statut TEXT NOT NULL DEFAULT 'Validee'; ALTER TABLE Vente ADD COLUMN AnnuleeLe TEXT NULL; ALTER TABLE Vente ADD COLUMN AnnuleeParId INTEGER NULL REFERENCES Utilisateur(Id); ALTER TABLE Vente ADD COLUMN MotifAnnulation TEXT NULL; CREATE INDEX IX_Vente_DateStatut ON Vente(DateVente,Statut); CREATE INDEX IX_Vente_VendeurDate ON Vente(VendeurId,DateVente); INSERT INTO VersionSchema VALUES(4,$date); PRAGMA user_version=4;";
        cmd.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O")); cmd.ExecuteNonQuery(); tx.Commit();
    }
    private static void ApplyVersion5(SqliteConnection c)
    {
        using var tx = c.BeginTransaction(); using var cmd = c.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_MouvementStock_Product ON MouvementStock(ProduitId); CREATE INDEX IF NOT EXISTS IX_LigneVente_Product ON LigneVente(ProduitId); INSERT INTO VersionSchema VALUES(5,$date); PRAGMA user_version=5;";
        cmd.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O")); cmd.ExecuteNonQuery(); tx.Commit();
    }
}
