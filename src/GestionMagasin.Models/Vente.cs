namespace GestionMagasin.Models;

public enum ModePaiement
{
    Especes,
    MobileMoney,
    Carte
}

public sealed record LigneVenteDemandee(long ProduitId, long Quantite, decimal PrixUnitaireAttendu);

public sealed record VenteValidee(
    long Id,
    string JetonValidation,
    DateTimeOffset DateVente,
    ModePaiement ModePaiement,
    decimal Total,
    decimal MontantRecu,
    decimal MonnaieRendue);
