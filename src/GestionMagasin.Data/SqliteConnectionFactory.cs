using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

/// <summary>Construit les connexions vers la base locale de l'utilisateur.</summary>
public sealed class SqliteConnectionFactory
{
    public string DatabasePath { get; }

    public SqliteConnectionFactory(string? databasePath = null)
    {
        DatabasePath = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GestionMagasin",
            "gestion-magasin.db");
    }

    public SqliteConnection CreateConnection()
    {
        var directory = Path.GetDirectoryName(DatabasePath)
            ?? throw new InvalidOperationException("Le chemin de la base de données est invalide.");
        Directory.CreateDirectory(directory);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            DefaultTimeout = 30
        };
        return new SqliteConnection(builder.ToString());
    }
}
