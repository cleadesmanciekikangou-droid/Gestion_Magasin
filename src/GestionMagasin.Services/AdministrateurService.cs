using GestionMagasin.Data;
using GestionMagasin.Models;

namespace GestionMagasin.Services;

/// <summary>Règles métier liées à la configuration du premier compte administrateur.</summary>
public sealed class AdministrateurService(IUtilisateurRepository repository, PasswordHasher passwordHasher)
{
    public bool AdministrateurExiste() => repository.AdministrateurExiste();

    public bool CreerPremierAdministrateur(string nom, string identifiant, string motDePasse)
    {
        nom = nom.Trim();
        identifiant = identifiant.Trim();
        if (nom.Length is < 2 or > 100)
            throw new ArgumentException("Le nom doit contenir entre 2 et 100 caractères.");
        if (identifiant.Length is < 3 or > 100)
            throw new ArgumentException("L’identifiant doit contenir entre 3 et 100 caractères.");
        if (motDePasse.Length < 12)
            throw new ArgumentException("Le mot de passe doit contenir au moins 12 caractères.");

        var result = passwordHasher.Hash(motDePasse);
        var admin = new Utilisateur
        {
            Nom = nom,
            Identifiant = identifiant,
            MotDePasseHash = result.Hash,
            Sel = result.Salt,
            ParametresHachage = result.Parameters,
            Role = "Administrateur",
            Actif = true
        };
        return repository.CreerPremierAdministrateur(admin);
    }
}
