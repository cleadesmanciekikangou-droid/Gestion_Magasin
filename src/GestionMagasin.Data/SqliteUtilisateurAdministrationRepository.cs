using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

/// <summary>Gestion des comptes et garde-fou transactionnel du dernier administrateur actif.</summary>
public sealed class SqliteUtilisateurAdministrationRepository(SqliteConnectionFactory factory)
{
    public IReadOnlyList<GestionMagasin.Services.CompteUtilisateur> Lister()
    {
        using var c = factory.CreateConnection(); c.Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,Nom,Identifiant,Role,Actif FROM Utilisateur ORDER BY Nom COLLATE NOCASE;";
        using var r = cmd.ExecuteReader(); var rows = new List<GestionMagasin.Services.CompteUtilisateur>();
        while (r.Read()) rows.Add(new(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt64(4) == 1)); return rows;
    }
    public long Creer(string name, string login, string hash, string salt, string parameters, string role)
    {
        using var c = factory.CreateConnection(); c.Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO Utilisateur(Nom,Identifiant,MotDePasseHash,Sel,ParametresHachage,Role,Actif) VALUES($name,$login,$hash,$salt,$parameters,$role,1); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$name", name); cmd.Parameters.AddWithValue("$login", login); cmd.Parameters.AddWithValue("$hash", hash); cmd.Parameters.AddWithValue("$salt", salt); cmd.Parameters.AddWithValue("$parameters", parameters); cmd.Parameters.AddWithValue("$role", role);
        try { return Convert.ToInt64(cmd.ExecuteScalar()); } catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { throw new InvalidOperationException("Identifiant déjà utilisé.", ex); }
    }
    public void DefinirActif(long actorId, long id, bool active)
    {
        using var c = factory.CreateConnection(); c.Open(); using var tx = c.BeginTransaction(deferred: false);
        using var role = c.CreateCommand(); role.Transaction = tx; role.CommandText = "SELECT Role,Actif FROM Utilisateur WHERE Id=$id;"; role.Parameters.AddWithValue("$id", id);
        using var r = role.ExecuteReader(); if (!r.Read()) throw new KeyNotFoundException("Utilisateur introuvable."); var isAdmin = r.GetString(0) == "Administrateur"; var wasActive = r.GetInt64(1) == 1; r.Close();
        if (!active && isAdmin && wasActive)
        {
            using var count = c.CreateCommand(); count.Transaction = tx; count.CommandText = "SELECT COUNT(*) FROM Utilisateur WHERE Role='Administrateur' AND Actif=1;";
            if (Convert.ToInt32(count.ExecuteScalar()) <= 1) throw new InvalidOperationException("Impossible de désactiver le dernier administrateur actif.");
        }
        using var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = "UPDATE Utilisateur SET Actif=$active WHERE Id=$id;"; cmd.Parameters.AddWithValue("$active", active ? 1 : 0); cmd.Parameters.AddWithValue("$id", id); cmd.ExecuteNonQuery(); tx.Commit();
    }
    public void ChangerMotDePasse(long id, string hash, string salt, string parameters)
    {
        using var c = factory.CreateConnection(); c.Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "UPDATE Utilisateur SET MotDePasseHash=$hash,Sel=$salt,ParametresHachage=$parameters WHERE Id=$id AND Actif=1;";
        cmd.Parameters.AddWithValue("$hash", hash); cmd.Parameters.AddWithValue("$salt", salt); cmd.Parameters.AddWithValue("$parameters", parameters); cmd.Parameters.AddWithValue("$id", id);
        if (cmd.ExecuteNonQuery() != 1) throw new KeyNotFoundException("Compte actif introuvable.");
    }
}
