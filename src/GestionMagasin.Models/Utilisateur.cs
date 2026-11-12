namespace GestionMagasin.Models;

/// <summary>Représente un compte utilisateur persisté en base.</summary>
public sealed class Utilisateur
{
    public long Id { get; set; }
    public required string Nom { get; init; }
    public required string Identifiant { get; init; }
    public required string MotDePasseHash { get; init; }
    public required string Sel { get; init; }
    public required string ParametresHachage { get; init; }
    public required string Role { get; init; }
    public bool Actif { get; init; }
}
