using GestionMagasin.Models;
using GestionMagasin.Services;

namespace GestionMagasin.WinForms;

internal sealed class SalesHistoryForm : Form
{
    private readonly RapportsService _reports; private readonly SessionUtilisateur _session;
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Short };
    private readonly ComboBox _vendor = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly ComboBox _status = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    public SalesHistoryForm(RapportsService reports, SessionUtilisateur session)
    {
        _reports = reports; _session = session; Text = "Historique des ventes"; StartPosition = FormStartPosition.CenterParent; Width = 1100; Height = 700;
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false, AutoScroll = true };
        _from.Value = DateTime.Today; _to.Value = DateTime.Today; _status.Items.AddRange(["Toutes", "Validee", "Annulee"]); _status.SelectedIndex = 0;
        if (session.EstAdministrateur) { _vendor.Items.Add(new VendorOption(null, "Tous les vendeurs")); foreach (var v in reports.Vendeurs(session)) _vendor.Items.Add(new VendorOption(v.Id, v.Nom)); _vendor.SelectedIndex = 0; toolbar.Controls.Add(_vendor); }
        toolbar.Controls.AddRange([new Label { Text = "Du", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, _from, new Label { Text = "Au", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, _to, _status, Btn("Filtrer", (_, _) => RefreshRows()), Btn("Aperçu / imprimer", (_, _) => Preview()), Btn("Exporter CSV", (_, _) => Export()), Btn("Annuler vente", (_, _) => Cancel())]);
        Controls.Add(_grid); Controls.Add(toolbar); RefreshRows();
    }
    private void RefreshRows()
    {
        try { var vendor = _vendor.SelectedItem is VendorOption option ? option.Id : null; _grid.DataSource = _reports.Ventes(_session, _from.Value.Date.ToUniversalTime(), _to.Value.Date.AddDays(1).ToUniversalTime(), vendor, Convert.ToString(_status.SelectedItem)).ToList(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Historique inaccessible", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private VenteHistorique? Selected => _grid.CurrentRow?.DataBoundItem as VenteHistorique;
    private void Preview() { if (Selected is not { } sale) return; using var form = new TicketPreviewForm(sale, _reports.Lignes(_session, sale.Id)); form.ShowDialog(this); }
    private void Cancel()
    {
        if (!_session.EstAdministrateur) { MessageBox.Show(this, "Réservé aux administrateurs."); return; }
        if (Selected is not { } sale) return;
        var reason = Microsoft.VisualBasic.Interaction.InputBox($"Motif obligatoire pour annuler la vente #{sale.Id} :", "Annulation de vente"); if (string.IsNullOrWhiteSpace(reason)) return;
        try { _reports.Annuler(_session, sale.Id, reason); RefreshRows(); MessageBox.Show(this, "Vente annulée; les mouvements de stock ont été compensés."); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Annulation refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private void Export()
    {
        using var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "ventes.csv" }; if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { CsvExporter.Ecrire(dialog.FileName, _reports.ExportVentes(_session, _from.Value.Date.ToUniversalTime(), _to.Value.Date.AddDays(1).ToUniversalTime())); MessageBox.Show(this, "Export créé."); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Export échoué", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private static Button Btn(string text, EventHandler handler) { var b = new Button { Text = text, AutoSize = true }; b.Click += handler; return b; }
    private sealed record VendorOption(long? Id, string Name) { public override string ToString() => Name; }
}
