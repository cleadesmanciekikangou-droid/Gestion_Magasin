using GestionMagasin.Models;

namespace GestionMagasin.Data;

public interface IStockRepository
{
    long ObtenirStock(long produitId);
    IReadOnlyList<MouvementStock> ObtenirMouvements(long? produitId = null, int limite = 500);
    long AjouterStockInitial(long produitId, long quantite, string motif);
    long Reapprovisionner(long produitId, long quantite, string motif);
    long EnregistrerVenteValidee(long produitId, long quantite, string referenceVente);
    long AnnulerVente(long mouvementVenteId, string motif);
    long CorrigerInventaire(long produitId, long quantiteComptee, string motif);
}
