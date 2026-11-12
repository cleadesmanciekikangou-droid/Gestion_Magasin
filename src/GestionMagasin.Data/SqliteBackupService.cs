using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

/// <summary>Backup SQLite cohérent via BackupDatabase, restauration validée sur copie temporaire.</summary>
public sealed class SqliteBackupService(SqliteConnectionFactory factory)
{
    public string BackupDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GestionMagasin", "Backups");

    public string CreerBackup(bool automatique)
    {
        Directory.CreateDirectory(BackupDirectory);
        var suffix = automatique ? "auto" : "manuel";
        var target = Path.Combine(BackupDirectory, $"gestion-magasin-{suffix}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.db");
        using var source = factory.CreateConnection(); source.Open();
        using var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = target, Mode = SqliteOpenMode.ReadWriteCreate }.ToString());
        destination.Open(); source.BackupDatabase(destination);
        VerifierBase(destination, DatabaseInitializer.SchemaVersion);
        if (automatique) RetenirSeptAutomatiques();
        return target;
    }

    public void Restaurer(string cheminBackup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cheminBackup);
        var sourcePath = Path.GetFullPath(cheminBackup);
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Fichier de sauvegarde introuvable.", sourcePath);
        var targetPath = Path.GetFullPath(factory.DatabasePath);
        if (string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Choisissez un fichier de sauvegarde différent de la base active.");

        var validationPath = Path.Combine(BackupDirectory, $"restore-check-{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(BackupDirectory);
        try
        {
            using (var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = sourcePath, Mode = SqliteOpenMode.ReadOnly }.ToString()))
            using (var validation = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = validationPath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString()))
            {
                source.Open(); validation.Open(); source.BackupDatabase(validation);
                VerifierBase(validation, DatabaseInitializer.SchemaVersion);
            }

            var rollback = Path.Combine(BackupDirectory, $"pre-restore-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.db");
            using (var active = factory.CreateConnection())
            using (var previous = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = rollback, Mode = SqliteOpenMode.ReadWriteCreate }.ToString()))
            {
                active.Open(); previous.Open(); active.BackupDatabase(previous);
            }

            try
            {
                using var checkedCopy = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = validationPath, Mode = SqliteOpenMode.ReadOnly }.ToString());
                using var destination = factory.CreateConnection(); checkedCopy.Open(); destination.Open(); checkedCopy.BackupDatabase(destination);
                VerifierBase(destination, DatabaseInitializer.SchemaVersion);
            }
            catch
            {
                using var old = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = rollback, Mode = SqliteOpenMode.ReadOnly }.ToString());
                using var destination = factory.CreateConnection(); old.Open(); destination.Open(); old.BackupDatabase(destination);
                throw;
            }
        }
        finally
        {
            try { if (File.Exists(validationPath)) File.Delete(validationPath); } catch { /* conserve le fichier temporaire au diagnostic */ }
        }
    }

    private static void VerifierBase(SqliteConnection connection, int maxVersion)
    {
        using var integrity = connection.CreateCommand(); integrity.CommandText = "PRAGMA integrity_check;";
        if (!string.Equals(Convert.ToString(integrity.ExecuteScalar()), "ok", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("La sauvegarde SQLite est corrompue.");
        using var version = connection.CreateCommand(); version.CommandText = "PRAGMA user_version;";
        var schema = Convert.ToInt32(version.ExecuteScalar());
        if (schema < 1 || schema > maxVersion) throw new InvalidDataException($"Version de schéma non prise en charge : {schema}.");
        using var fk = connection.CreateCommand(); fk.CommandText = "PRAGMA foreign_key_check;";
        using var reader = fk.ExecuteReader();
        if (reader.Read()) throw new InvalidDataException("La sauvegarde contient des références étrangères invalides.");
    }

    private void RetenirSeptAutomatiques()
    {
        foreach (var file in new DirectoryInfo(BackupDirectory).GetFiles("gestion-magasin-auto-*.db").OrderByDescending(f => f.CreationTimeUtc).Skip(7))
            try { file.Delete(); } catch (IOException) { /* réessai au prochain backup */ }
    }
}
