using GestionMagasin.Models;
using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

/// <summary>Accès paramétré aux comptes utilisateur.</summary>
public sealed class SqliteUtilisateurRepository(SqliteConnectionFactory connectionFactory) : IUtilisateurRepository
{
    public bool AdministrateurExiste()
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM Utilisateur WHERE Role = $role AND Actif = 1);";
        command.Parameters.AddWithValue("$role", "Administrateur");
        return Convert.ToInt64(command.ExecuteScalar()) == 1;
    }

    public bool CreerPremierAdministrateur(Utilisateur utilisateur)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using (var check = connection.CreateCommand())
        {
            check.Transaction = transaction;
            check.CommandText = "SELECT EXISTS(SELECT 1 FROM Utilisateur WHERE Role = $role);";
            check.Parameters.AddWithValue("$role", "Administrateur");
            if (Convert.ToInt64(check.ExecuteScalar()) == 1)
            {
                transaction.Rollback();
                return false;
            }
        }

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO Utilisateur (Nom, Identifiant, MotDePasseHash, Sel, ParametresHachage, Role, Actif)
            VALUES ($nom, $identifiant, $hash, $sel, $parametres, $role, 1);
            """;
        insert.Parameters.AddWithValue("$nom", utilisateur.Nom);
        insert.Parameters.AddWithValue("$identifiant", utilisateur.Identifiant);
        insert.Parameters.AddWithValue("$hash", utilisateur.MotDePasseHash);
        insert.Parameters.AddWithValue("$sel", utilisateur.Sel);
        insert.Parameters.AddWithValue("$parametres", utilisateur.ParametresHachage);
        insert.Parameters.AddWithValue("$role", utilisateur.Role);
        insert.ExecuteNonQuery();
        transaction.Commit();
        return true;
    }
}
