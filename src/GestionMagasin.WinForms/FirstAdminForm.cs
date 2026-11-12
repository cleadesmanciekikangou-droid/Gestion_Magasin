using GestionMagasin.Services;
using Microsoft.Data.Sqlite;

namespace GestionMagasin.WinForms;

/// <summary>Assistant obligatoire de création du premier administrateur.</summary>
internal sealed class FirstAdminForm : Form
{
    private readonly AdministrateurService _service;
    private readonly TextBox _name = new();
    private readonly TextBox _identifier = new();
    private readonly TextBox _password = new();
    private readonly TextBox _confirm = new();

    public FirstAdminForm(AdministrateurService service)
    {
        _service = service;
        Text = "Configuration initiale — Gestion Magasin";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(480, 360);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 2,
            RowCount = 6,
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        for (var i = 1; i < 5; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var intro = new Label
        {
            Text = "Créez le compte administrateur initial. Aucun mot de passe par défaut n'est utilisé.",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(intro, 0, 0);
        layout.SetColumnSpan(intro, 2);

        AddField(layout, "Nom", _name, 1);
        AddField(layout, "Identifiant", _identifier, 2);
        _password.UseSystemPasswordChar = true;
        _confirm.UseSystemPasswordChar = true;
        AddField(layout, "Mot de passe", _password, 3);
        AddField(layout, "Confirmer", _confirm, 4);

        var create = new Button { Text = "Créer l’administrateur", AutoSize = true, Anchor = AnchorStyles.Right };
        create.Click += CreateAdmin;
        layout.Controls.Add(create, 1, 5);
        AcceptButton = create;
        Controls.Add(layout);
    }

    private static void AddField(TableLayoutPanel layout, string labelText, TextBox textBox, int row)
    {
        layout.Controls.Add(new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        textBox.Dock = DockStyle.Fill;
        layout.Controls.Add(textBox, 1, row);
    }

    private void CreateAdmin(object? sender, EventArgs e)
    {
        if (!string.Equals(_password.Text, _confirm.Text, StringComparison.Ordinal))
        {
            MessageBox.Show(this, "Les mots de passe ne correspondent pas.", "Vérification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            if (!_service.CreerPremierAdministrateur(_name.Text, _identifier.Text, _password.Text))
            {
                MessageBox.Show(this, "Un administrateur a déjà été créé.", "Configuration terminée", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
                return;
            }
            MessageBox.Show(this, "Le compte administrateur a été créé.", "Configuration terminée", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ArgumentException exception)
        {
            MessageBox.Show(this, exception.Message, "Informations invalides", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            MessageBox.Show(this, "Cet identifiant est déjà utilisé. Choisissez-en un autre.", "Identifiant indisponible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"La création du compte a échoué.\n\n{exception.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
