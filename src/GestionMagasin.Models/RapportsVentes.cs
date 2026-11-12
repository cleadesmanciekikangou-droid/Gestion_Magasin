namespace GestionMagasin.Models;

public sealed record SessionUtilisateur(long Id, string Nom, string Identifiant, string Role)
{
    public bool EstAdministrateur => string.Equals(Role, "Administrateur", StringComparison.OrdinalIgnoreCase);
}

public sealed record ResumeTableauBord(DateOnly DateMetier, decimal TotalVentes, int NombreVentes, long UnitesVendues, int ProduitsEnRupture);
public sealed record VenteHistorique(long Id, DateTimeOffset DateVente, string Vendeur, string ModePaiement, decimal Total, string Statut, string? MotifAnnulation);
public sealed record LigneVenteDetail(string Reference, string Nom, decimal PrixUnitaire, long Quantite, decimal Total);
public sealed record PerformanceProduit(string Reference, string Nom, long UnitesVendues, decimal ChiffreAffaires);
