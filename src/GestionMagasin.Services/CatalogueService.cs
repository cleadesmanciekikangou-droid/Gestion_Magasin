using GestionMagasin.Models;

namespace GestionMagasin.Services;

public enum StatutAlerteStock { Rupture, Faible, Disponible }
public sealed record AlerteStock(StatutAlerteStock Statut, string Libelle, string Couleur);

public static class ReglesStock
{
    public static AlerteStock Evaluer(long stock, int seuil)
    {
        if (stock < 0) throw new InvalidOperationException("Un stock négatif indique une incohérence des mouvements.");
        if (seuil < 0) throw new ArgumentOutOfRangeException(nameof(seuil));
        return stock == 0 ? new(StatutAlerteStock.Rupture, "En rupture", "Red")
            : stock <= seuil ? new(StatutAlerteStock.Faible, "Stock faible", "DarkOrange")
            : new(StatutAlerteStock.Disponible, "Disponible", "ForestGreen");
    }
}

public sealed class CatalogueService(GestionMagasin.Data.ICatalogueRepository catalogue, GestionMagasin.Data.IStockRepository stock)
{
    public IReadOnlyList<Produit> Rechercher(string? texte, long? categorie, bool? actif) => catalogue.RechercherProduits(texte, categorie, actif);
    public IReadOnlyList<GestionMagasin.Models.Categorie> Categories() => catalogue.ObtenirCategories();
    public long CreerCategorie(string nom) => catalogue.CreerCategorie(nom);
    public long CreerProduit(string reference, string nom, long categorie, decimal prix, int seuil) => catalogue.CreerProduit(reference, nom, categorie, prix, seuil);
    public void ModifierProduit(long id, string reference, string nom, long categorie, decimal prix, int seuil) => catalogue.ModifierProduit(id, reference, nom, categorie, prix, seuil);
    public void Archiver(long id) => catalogue.DefinirProduitActif(id, false);
    public void Reactiver(long id) => catalogue.DefinirProduitActif(id, true);
}

public sealed class StockService(GestionMagasin.Data.IStockRepository repository)
{
    public long Stock(long produitId) => repository.ObtenirStock(produitId);
    public IReadOnlyList<MouvementStock> Mouvements(long? produitId = null) => repository.ObtenirMouvements(produitId);
    public long Initialiser(long produitId, long quantite, string motif) => repository.AjouterStockInitial(produitId, quantite, motif);
    public long Reapprovisionner(long produitId, long quantite, string motif) => repository.Reapprovisionner(produitId, quantite, motif);
    public long Vendre(long produitId, long quantite, string referenceVente) => repository.EnregistrerVenteValidee(produitId, quantite, referenceVente);
    public long AnnulerVente(long mouvementId, string motif) => repository.AnnulerVente(mouvementId, motif);
    public long Corriger(long produitId, long compte, string motif) => repository.CorrigerInventaire(produitId, compte, motif);
}
