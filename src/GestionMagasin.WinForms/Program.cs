using GestionMagasin.Data;
using GestionMagasin.Services;

namespace GestionMagasin.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            var connectionFactory = new SqliteConnectionFactory();
            new DatabaseInitializer(connectionFactory).Initialize();
            var adminService = new AdministrateurService(new SqliteUtilisateurRepository(connectionFactory), new PasswordHasher());
            if (!adminService.AdministrateurExiste())
            {
                using var setup = new FirstAdminForm(adminService);
                if (setup.ShowDialog() != DialogResult.OK) return;
            }
            var catalogue = new CatalogueService(new SqliteCatalogueRepository(connectionFactory), new SqliteStockRepository(connectionFactory));
            var stock = new StockService(new SqliteStockRepository(connectionFactory));
            Application.Run(new MainForm(connectionFactory.DatabasePath, catalogue, stock));
        }
        catch (Exception exception)
        {
            MessageBox.Show($"L'application n'a pas pu démarrer.\n\n{exception.Message}", "Erreur de démarrage", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
