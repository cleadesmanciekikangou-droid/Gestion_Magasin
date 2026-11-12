using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

/// <summary>Persistance minimale des réglages de magasin.</summary>
public sealed class SqliteStoreSettingsRepository(SqliteConnectionFactory factory)
{
    public IReadOnlyDictionary<string, string> LireTout()
    {
        using var c = factory.CreateConnection(); c.Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Cle,Valeur FROM Parametre ORDER BY Cle;";
        using var r = cmd.ExecuteReader(); var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (r.Read()) values[r.GetString(0)] = r.GetString(1);
        return values;
    }

    public void Enregistrer(string name, string logoPath, string currency)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120) throw new ArgumentException("Le nom du magasin est obligatoire (120 caractères maximum).");
        if (currency.Trim().Length is < 3 or > 8) throw new ArgumentException("Code devise invalide.");
        using var c = factory.CreateConnection(); c.Open(); using var tx = c.BeginTransaction();
        foreach (var pair in new[] { ("Magasin.Nom", name.Trim()), ("Magasin.Logo", logoPath.Trim()), ("Magasin.Devise", currency.Trim().ToUpperInvariant()) })
        {
            using var cmd = c.CreateCommand(); cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO Parametre(Cle,Valeur,ModifieLe) VALUES($key,$value,$date) ON CONFLICT(Cle) DO UPDATE SET Valeur=excluded.Valeur,ModifieLe=excluded.ModifieLe;";
            cmd.Parameters.AddWithValue("$key", pair.Item1); cmd.Parameters.AddWithValue("$value", pair.Item2); cmd.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O")); cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }
}
