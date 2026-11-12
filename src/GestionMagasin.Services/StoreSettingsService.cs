using GestionMagasin.Models;

namespace GestionMagasin.Services;

public sealed class StoreSettingsService(GestionMagasin.Data.SqliteStoreSettingsRepository repository)
{
    public IReadOnlyDictionary<string, string> Lire(SessionUtilisateur actor) { AdministrationService.RequireAdmin(actor); return repository.LireTout(); }
    public void Enregistrer(SessionUtilisateur actor, string nom, string logo, string devise) { AdministrationService.RequireAdmin(actor); repository.Enregistrer(nom, logo, devise); }
}
