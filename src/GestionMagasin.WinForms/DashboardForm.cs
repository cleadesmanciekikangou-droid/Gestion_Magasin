using GestionMagasin.Models;
using GestionMagasin.Services;

namespace GestionMagasin.WinForms;

internal sealed class DashboardForm : Form
{
    public DashboardForm(RapportsService reports, SessionUtilisateur session)
    {
        Text = "Tableau de bord"; StartPosition = FormStartPosition.CenterParent; Width = 800; Height = 560;
        var date = DateOnly.FromDateTime(DateTime.Today); var summary = reports.TableauBord(session, date); var rows = reports.RapportJournalier(session, date);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 6 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        AddMetric(layout, "Date métier", summary.DateMetier.ToString("dd/MM/yyyy"), 0);
        AddMetric(layout, "Chiffre d'affaires du jour", summary.TotalVentes.ToString("N2"), 1);
        AddMetric(layout, "Nombre de ventes", summary.NombreVentes.ToString(), 2);
        AddMetric(layout, "Unités vendues", summary.UnitesVendues.ToString(), 3);
        AddMetric(layout, "Produits en rupture", summary.ProduitsEnRupture.ToString(), 4);
        var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, DataSource = rows.ToList() };
        layout.Controls.Add(new Label { Text = "Performance des produits — journée 00:00 à 00:00", AutoSize = true }, 0, 5); layout.SetColumnSpan(layout.GetControlFromPosition(0, 5)!, 2);
        Controls.Add(layout); var host = new Panel { Dock = DockStyle.Fill }; Controls.Add(host); host.BringToFront(); host.Controls.Add(grid);
    }
    private static void AddMetric(TableLayoutPanel layout, string title, string value, int row)
    { layout.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, row); layout.Controls.Add(new Label { Text = value, AutoSize = true, TextAlign = ContentAlignment.MiddleRight }, 1, row); }
}
