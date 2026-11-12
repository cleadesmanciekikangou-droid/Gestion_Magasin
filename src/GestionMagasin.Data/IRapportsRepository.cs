using GestionMagasin.Models;

namespace GestionMagasin.Data;

public interface IRapportsRepository
{
    ResumeTableauBord ObtenirTableauBord(SessionUtilisateur session, DateOnly dateMetier);
    IReadOnlyList<VenteHistorique> RechercherVentes(SessionUtilisateur session, DateTimeOffset duInclus, DateTimeOffset auExclus, long? vendeurId, string? statut);
    IReadOnlyList<LigneVenteDetail> ObtenirLignesVente(SessionUtilisateur session, long venteId);
    IReadOnlyList<PerformanceProduit> RapportProduitsJournalier(SessionUtilisateur session, DateOnly dateMetier);
    void AnnulerVente(SessionUtilisateur session, long venteId, string motif);
    IReadOnlyList<string[]> ExportVentes(SessionUtilisateur session, DateTimeOffset duInclus, DateTimeOffset auExclus);
    IReadOnlyList<string[]> ExportInventaire(SessionUtilisateur session);
    IReadOnlyList<(long Id, string Nom)> ObtenirVendeurs(SessionUtilisateur session);
}
