using GestionMagasin.Models;

namespace GestionMagasin.Data;

public interface IVenteRepository
{
    VenteValidee ValiderVente(
        string jetonValidation,
        IReadOnlyList<LigneVenteDemandee> lignes,
        ModePaiement modePaiement,
        decimal montantRecu);
}
