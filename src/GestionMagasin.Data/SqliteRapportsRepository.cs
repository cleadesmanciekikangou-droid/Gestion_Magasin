using System.Globalization;
using GestionMagasin.Models;
using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

/// <summary>Rapports et annulations transactionnels avec contrôles d'accès effectués en base.</summary>
public sealed class SqliteRapportsRepository(SqliteConnectionFactory factory) : IRapportsRepository
{
    public ResumeTableauBord ObtenirTableauBord(SessionUtilisateur session, DateOnly dateMetier)
    {
        using var c = Open();
        var start = LocalStart(dateMetier).ToString("O"); var end = LocalStart(dateMetier.AddDays(1)).ToString("O");
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT COALESCE(SUM(v.Total),0), COUNT(*), COALESCE((SELECT SUM(l.Quantite) FROM LigneVente l JOIN Vente x ON x.Id=l.VenteId WHERE x.Statut='Validee' AND x.DateVente >= $start AND x.DateVente < $end AND ($admin=1 OR x.VendeurId=$user)),0)
            FROM Vente v WHERE v.Statut='Validee' AND v.DateVente >= $start AND v.DateVente < $end AND ($admin=1 OR v.VendeurId=$user);
            """;
        AddAccess(cmd, session); cmd.Parameters.AddWithValue("$start", start); cmd.Parameters.AddWithValue("$end", end);
        using var r = cmd.ExecuteReader(); r.Read(); var total = Convert.ToDecimal(r.GetValue(0), CultureInfo.InvariantCulture); var count = r.GetInt32(1); var units = Convert.ToInt64(r.GetValue(2)); r.Close();
        using var outOfStock = c.CreateCommand(); outOfStock.CommandText = "SELECT COUNT(*) FROM Produit p WHERE p.Actif=1 AND COALESCE((SELECT SUM(Quantite) FROM MouvementStock m WHERE m.ProduitId=p.Id),0)=0;";
        return new ResumeTableauBord(dateMetier, total, count, units, Convert.ToInt32(outOfStock.ExecuteScalar(), CultureInfo.InvariantCulture));
    }

    public IReadOnlyList<VenteHistorique> RechercherVentes(SessionUtilisateur session, DateTimeOffset duInclus, DateTimeOffset auExclus, long? vendeurId, string? statut)
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT v.Id, v.DateVente, COALESCE(u.Nom,'(ancien vendeur)'), v.ModePaiement, v.Total, v.Statut, v.MotifAnnulation
            FROM Vente v LEFT JOIN Utilisateur u ON u.Id=v.VendeurId
            WHERE v.DateVente >= $start AND v.DateVente < $end AND ($admin=1 OR v.VendeurId=$user)
              AND ($vendor IS NULL OR v.VendeurId=$vendor) AND ($status IS NULL OR v.Statut=$status)
            ORDER BY v.DateVente DESC LIMIT 5000;
            """;
        AddAccess(cmd, session); cmd.Parameters.AddWithValue("$start", duInclus.ToUniversalTime().ToString("O")); cmd.Parameters.AddWithValue("$end", auExclus.ToUniversalTime().ToString("O"));
        cmd.Parameters.AddWithValue("$vendor", (object?)vendeurId ?? DBNull.Value); cmd.Parameters.AddWithValue("$status", (object?)NormalizeStatus(statut) ?? DBNull.Value);
        using var r = cmd.ExecuteReader(); var list = new List<VenteHistorique>();
        while (r.Read()) list.Add(new VenteHistorique(r.GetInt64(0), DateTimeOffset.Parse(r.GetString(1), CultureInfo.InvariantCulture), r.GetString(2), r.GetString(3), Convert.ToDecimal(r.GetValue(4), CultureInfo.InvariantCulture), r.GetString(5), r.IsDBNull(6) ? null : r.GetString(6)));
        return list;
    }

    public IReadOnlyList<LigneVenteDetail> ObtenirLignesVente(SessionUtilisateur session, long venteId)
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT l.ReferenceProduitSnapshot,l.NomProduitSnapshot,l.PrixUnitaireSnapshot,l.Quantite,l.TotalLigne FROM LigneVente l JOIN Vente v ON v.Id=l.VenteId WHERE v.Id=$id AND ($admin=1 OR v.VendeurId=$user) ORDER BY l.Id;";
        AddAccess(cmd, session); cmd.Parameters.AddWithValue("$id", venteId); using var r = cmd.ExecuteReader(); var rows = new List<LigneVenteDetail>();
        while (r.Read()) rows.Add(new LigneVenteDetail(r.GetString(0), r.GetString(1), Convert.ToDecimal(r.GetValue(2), CultureInfo.InvariantCulture), r.GetInt64(3), Convert.ToDecimal(r.GetValue(4), CultureInfo.InvariantCulture)));
        return rows;
    }

    public IReadOnlyList<PerformanceProduit> RapportProduitsJournalier(SessionUtilisateur session, DateOnly dateMetier)
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT l.ReferenceProduitSnapshot,l.NomProduitSnapshot,SUM(l.Quantite),SUM(l.TotalLigne)
            FROM LigneVente l JOIN Vente v ON v.Id=l.VenteId
            WHERE v.Statut='Validee' AND v.DateVente >= $start AND v.DateVente < $end AND ($admin=1 OR v.VendeurId=$user)
            GROUP BY l.ReferenceProduitSnapshot,l.NomProduitSnapshot ORDER BY SUM(l.Quantite) DESC;
            """;
        AddAccess(cmd, session); cmd.Parameters.AddWithValue("$start", LocalStart(dateMetier).ToString("O")); cmd.Parameters.AddWithValue("$end", LocalStart(dateMetier.AddDays(1)).ToString("O"));
        using var r = cmd.ExecuteReader(); var rows = new List<PerformanceProduit>();
        while (r.Read()) rows.Add(new PerformanceProduit(r.GetString(0), r.GetString(1), r.GetInt64(2), Convert.ToDecimal(r.GetValue(3), CultureInfo.InvariantCulture)));
        return rows;
    }

    public void AnnulerVente(SessionUtilisateur session, long venteId, string motif)
    {
        if (string.IsNullOrWhiteSpace(motif) || motif.Trim().Length > 300) throw new ArgumentException("Un motif obligatoire de 1 à 300 caractères est requis.");
        using var c = Open(); using var tx = c.BeginTransaction(deferred: false); EnsureAdministrator(c, tx, session);
        using (var state = c.CreateCommand())
        {
            state.Transaction = tx; state.CommandText = "SELECT Statut FROM Vente WHERE Id=$id;"; state.Parameters.AddWithValue("$id", venteId);
            var value = state.ExecuteScalar() as string;
            if (value is null) throw new KeyNotFoundException("Vente introuvable.");
            if (value != "Validee") throw new InvalidOperationException("Seule une vente validée peut être annulée; les doubles annulations sont interdites.");
        }
        using (var movements = c.CreateCommand())
        {
            movements.Transaction = tx;
            movements.CommandText = "INSERT INTO MouvementStock (ProduitId,Type,Quantite,Motif,DateMouvement,MouvementOrigineId,VenteId) SELECT ProduitId,'AnnulationVente',-Quantite,$reason,$date,Id,VenteId FROM MouvementStock WHERE VenteId=$sale AND Type='VenteValidee';";
            movements.Parameters.AddWithValue("$reason", "Annulation vente #" + venteId + " : " + motif.Trim()); movements.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O")); movements.Parameters.AddWithValue("$sale", venteId); movements.ExecuteNonQuery();
        }
        using (var update = c.CreateCommand())
        {
            update.Transaction = tx; update.CommandText = "UPDATE Vente SET Statut='Annulee',AnnuleeLe=$date,AnnuleeParId=$user,MotifAnnulation=$reason WHERE Id=$id AND Statut='Validee';";
            update.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O")); update.Parameters.AddWithValue("$user", session.Id); update.Parameters.AddWithValue("$reason", motif.Trim()); update.Parameters.AddWithValue("$id", venteId);
            if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("La vente a déjà été annulée.");
        }
        tx.Commit();
    }

    public IReadOnlyList<string[]> ExportVentes(SessionUtilisateur session, DateTimeOffset duInclus, DateTimeOffset auExclus)
    {
        using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT v.Id,v.DateVente,COALESCE(u.Nom,''),v.ModePaiement,v.Total,v.Statut,COALESCE(v.MotifAnnulation,'') FROM Vente v LEFT JOIN Utilisateur u ON u.Id=v.VendeurId WHERE v.DateVente >= $start AND v.DateVente < $end AND ($admin=1 OR v.VendeurId=$user) ORDER BY v.DateVente;";
        AddAccess(cmd, session); cmd.Parameters.AddWithValue("$start", duInclus.ToUniversalTime().ToString("O")); cmd.Parameters.AddWithValue("$end", auExclus.ToUniversalTime().ToString("O"));
        using var r = cmd.ExecuteReader(); var result = new List<string[]> { ["Id", "Date", "Vendeur", "Paiement", "Total", "Statut", "Motif annulation"] };
        while (r.Read()) result.Add(Enumerable.Range(0, 7).Select(i => r.IsDBNull(i) ? "" : Convert.ToString(r.GetValue(i), CultureInfo.InvariantCulture) ?? "").ToArray()); return result;
    }

    public IReadOnlyList<string[]> ExportInventaire(SessionUtilisateur session)
    {
        EnsureSession(session); using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT p.Reference,p.Nom,c.Nom,p.PrixVente,p.SeuilAlerte,COALESCE(SUM(m.Quantite),0),p.Actif FROM Produit p JOIN Categorie c ON c.Id=p.CategorieId LEFT JOIN MouvementStock m ON m.ProduitId=p.Id GROUP BY p.Id ORDER BY p.Nom;";
        using var r = cmd.ExecuteReader(); var result = new List<string[]> { ["Référence", "Produit", "Catégorie", "Prix", "Seuil", "Stock", "Actif"] };
        while (r.Read()) result.Add(Enumerable.Range(0, 7).Select(i => Convert.ToString(r.GetValue(i), CultureInfo.InvariantCulture) ?? "").ToArray()); return result;
    }

    public IReadOnlyList<(long Id, string Nom)> ObtenirVendeurs(SessionUtilisateur session)
    {
        EnsureAdministrator(session); using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT Id,Nom FROM Utilisateur WHERE Actif=1 AND Role='Vendeur' ORDER BY Nom;";
        using var r = cmd.ExecuteReader(); var result = new List<(long, string)>(); while (r.Read()) result.Add((r.GetInt64(0), r.GetString(1))); return result;
    }

    private SqliteConnection Open() { var c = factory.CreateConnection(); c.Open(); return c; }
    private static DateTimeOffset LocalStart(DateOnly d) => new(d.ToDateTime(TimeOnly.MinValue), TimeZoneInfo.Local.GetUtcOffset(d.ToDateTime(TimeOnly.MinValue)));
    private static string? NormalizeStatus(string? value) => string.IsNullOrWhiteSpace(value) || value == "Toutes" ? null : value;
    private static void EnsureSession(SessionUtilisateur s) { if (s.Id <= 0) throw new UnauthorizedAccessException("Session utilisateur invalide."); }
    private static void AddAccess(SqliteCommand cmd, SessionUtilisateur s) { EnsureSession(s); cmd.Parameters.AddWithValue("$user", s.Id); cmd.Parameters.AddWithValue("$admin", s.EstAdministrateur ? 1 : 0); }
    private static void EnsureAdministrator(SqliteConnection c, SqliteTransaction tx, SessionUtilisateur s)
    {
        EnsureSession(s); using var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = "SELECT Role FROM Utilisateur WHERE Id=$id AND Actif=1;"; cmd.Parameters.AddWithValue("$id", s.Id);
        if (!string.Equals(cmd.ExecuteScalar() as string, "Administrateur", StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("Seul un administrateur actif peut annuler une vente.");
    }
    private void EnsureAdministrator(SessionUtilisateur s) { using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT Role FROM Utilisateur WHERE Id=$id AND Actif=1;"; cmd.Parameters.AddWithValue("$id", s.Id); if (!string.Equals(cmd.ExecuteScalar() as string, "Administrateur", StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("Accès réservé à l'administrateur."); }
}
