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
            var adminService = new AdministrateurService(
                new SqliteUtilisateurRepository(connectionFactory),
                new PasswordHasher());

            if (!adminService.AdministrateurExiste())
            {
                using var setup = new FirstAdminForm(adminService);
                if (setup.ShowDialog() != DialogResult.OK)
                    return; // Aucun accès à l'application tant que l'administrateur n'est pas créé.
            }

            Application.Run(new MainForm(connectionFactory.DatabasePath));
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"L'application n'a pas pu démarrer.\n\n{exception.Message}",
                "Erreur de démarrage", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
