using System.Security.Cryptography;
using System.Text.Json;
using GestionMagasin.Models;

namespace GestionMagasin.Services;

public sealed record CompteUtilisateur(long Id, string Nom, string Identifiant, string Role, bool Actif);

/// <summary>Contrôle les rôles côté service métier; ne jamais se fier à la visibilité des boutons UI.</summary>
public sealed class AdministrationService(GestionMagasin.Data.SqliteUtilisateurAdministrationRepository users, PasswordHasher hasher)
{
    public IReadOnlyList<CompteUtilisateur> Lister(SessionUtilisateur actor) { RequireAdmin(actor); return users.Lister(); }
    public long Creer(SessionUtilisateur actor, string nom, string login, string password, string role)
    {
        RequireAdmin(actor);
        if (nom.Trim().Length is < 2 or > 100 || login.Trim().Length is < 3 or > 100) throw new ArgumentException("Nom ou identifiant invalide.");
        if (password.Length < 12) throw new ArgumentException("Le mot de passe doit comporter au moins 12 caractères.");
        if (role is not ("Administrateur" or "Vendeur")) throw new ArgumentException("Rôle inconnu.");
        var hash = hasher.Hash(password);
        return users.Creer(nom.Trim(), login.Trim(), hash.Hash, hash.Salt, hash.Parameters, role);
    }
    public void DefinirActif(SessionUtilisateur actor, long userId, bool actif) { RequireAdmin(actor); users.DefinirActif(actor.Id, userId, actif); }
    public void ChangerMotDePasse(SessionUtilisateur actor, long userId, string password)
    {
        RequireAdmin(actor); if (password.Length < 12) throw new ArgumentException("Le mot de passe doit comporter au moins 12 caractères.");
        var hash = hasher.Hash(password); users.ChangerMotDePasse(userId, hash.Hash, hash.Salt, hash.Parameters);
    }
    public static void RequireAdmin(SessionUtilisateur actor)
    {
        if (actor is null || actor.Id <= 0 || !actor.EstAdministrateur) throw new UnauthorizedAccessException("Action réservée à un administrateur.");
    }
}

public sealed class BackupService(GestionMagasin.Data.SqliteBackupService backup, AdministrationService _)
{
    public string CreerManuel(SessionUtilisateur actor) { AdministrationService.RequireAdmin(actor); return backup.CreerBackup(false); }
    public void Restaurer(SessionUtilisateur actor, string path) { AdministrationService.RequireAdmin(actor); backup.Restaurer(path); }
}
