namespace GestionMagasin.WinForms;

/// <summary>Fenêtre principale initiale à compléter avec les modules de gestion du magasin.</summary>
internal sealed class MainForm : Form
{
    public MainForm(string databasePath)
    {
        Text = "Gestion Magasin — Cosmétiques";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 16),
            Text = $"Application initialisée.\nBase locale : {databasePath}"
        });
    }
}
