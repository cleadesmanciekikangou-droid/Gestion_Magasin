using System.Globalization;
using System.Text;

namespace GestionMagasin.Services;

/// <summary>CSV RFC4180 avec protection contre l'interprétation de texte comme formule de tableur.</summary>
public static class CsvExporter
{
    public static void Ecrire(string path, IEnumerable<string[]> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        foreach (var row in rows) writer.WriteLine(string.Join(",", row.Select(Escape)));
    }

    private static string Escape(string value)
    {
        value ??= string.Empty;
        var safe = value.TrimStart() is { Length: > 0 } trimmed && trimmed[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
            ? "'" + value : value;
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }
}
