namespace GestionMagasin.Models;

public enum TypeMouvementStock
{
    StockInitial,
    Reapprovisionnement,
    VenteValidee,
    AnnulationVente,
    CorrectionInventaire
}

/// <summary>Quantite est signée : entrées positives, sorties négatives.</summary>
public sealed record MouvementStock(
    long Id,
    long ProduitId,
    string ReferenceProduit,
    string NomProduit,
    TypeMouvementStock Type,
    long Quantite,
    string Motif,
    DateTimeOffset DateMouvement,
    long? MouvementOrigineId);
