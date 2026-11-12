using GestionMagasin.Models;

namespace GestionMagasin.Data;

public interface ICatalogueRepository
{
    IReadOnlyList<Produit> RechercherProduits(string? recherche = null, long? categorieId = null, bool? actif = null);
    IReadOnlyList<Categorie> ObtenirCategories();
    long CreerCategorie(string nom);
    long CreerProduit(string reference, string nom, long categorieId, decimal prixVente, int seuilAlerte);
    void ModifierProduit(long id, string reference, string nom, long categorieId, decimal prixVente, int seuilAlerte);
    void DefinirProduitActif(long id, bool actif);
}
