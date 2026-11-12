using GestionMagasin.Data;
using GestionMagasin.Models;

namespace GestionMagasin.Services;

public sealed class RapportsService(IRapportsRepository repository)
{
    public ResumeTableauBord TableauBord(SessionUtilisateur session, DateOnly date) => repository.ObtenirTableauBord(session, date);
    public IReadOnlyList<VenteHistorique> Ventes(SessionUtilisateur session, DateTimeOffset debut, DateTimeOffset fin, long? vendeur, string? statut) => repository.RechercherVentes(session, debut, fin, vendeur, statut);
    public IReadOnlyList<LigneVenteDetail> Lignes(SessionUtilisateur session, long id) => repository.ObtenirLignesVente(session, id);
    public IReadOnlyList<PerformanceProduit> RapportJournalier(SessionUtilisateur session, DateOnly date) => repository.RapportProduitsJournalier(session, date);
    public void Annuler(SessionUtilisateur session, long id, string motif) => repository.AnnulerVente(session, id, motif);
    public IReadOnlyList<string[]> ExportVentes(SessionUtilisateur session, DateTimeOffset debut, DateTimeOffset fin) => repository.ExportVentes(session, debut, fin);
    public IReadOnlyList<string[]> ExportInventaire(SessionUtilisateur session) => repository.ExportInventaire(session);
    public IReadOnlyList<(long Id, string Nom)> Vendeurs(SessionUtilisateur session) => repository.ObtenirVendeurs(session);
}
