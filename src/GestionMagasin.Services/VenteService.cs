using GestionMagasin.Data;
using GestionMagasin.Models;

namespace GestionMagasin.Services;

/// <summary>Validation métier légère; la vérification finale est toujours refaite sous transaction SQLite.</summary>
public sealed class VenteService(IVenteRepository repository)
{
    public VenteValidee Valider(
        string jetonValidation,
        IReadOnlyList<LigneVenteDemandee> lignes,
        ModePaiement modePaiement,
        decimal montantRecu)
    {
        if (!Enum.IsDefined(modePaiement)) throw new ArgumentOutOfRangeException(nameof(modePaiement));
        if (lignes is null || lignes.Count == 0) throw new ArgumentException("Le panier est vide.", nameof(lignes));
        return repository.ValiderVente(jetonValidation, lignes, modePaiement, montantRecu);
    }
}
