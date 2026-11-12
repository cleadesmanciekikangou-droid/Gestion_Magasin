using GestionMagasin.Models;

namespace GestionMagasin.Data;

public interface IUtilisateurRepository
{
    bool AdministrateurExiste();
    /// <summary>Insère le premier administrateur uniquement si aucun n'existe.</summary>
    bool CreerPremierAdministrateur(Utilisateur utilisateur);
}
