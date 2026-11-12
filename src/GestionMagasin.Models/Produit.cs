namespace GestionMagasin.Models;

public sealed class Produit
{
    public long Id { get; set; }
    public required string Reference { get; init; }
    public required string Nom { get; init; }
    public long CategorieId { get; init; }
    public required string CategorieNom { get; init; }
    public decimal PrixVente { get; init; }
    public int SeuilAlerte { get; init; }
    public bool Actif { get; init; }
    public DateTimeOffset DateCreation { get; init; }
    public long Stock { get; init; }
    public string StatutStock => Stock == 0 ? "En rupture" : Stock <= SeuilAlerte ? "Stock faible" : "Disponible";
}

public sealed record Categorie(long Id, string Nom);
