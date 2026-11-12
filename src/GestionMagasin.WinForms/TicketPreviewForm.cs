using System.Drawing.Printing;
using System.Text;
using GestionMagasin.Models;
using GestionMagasin.Services;

namespace GestionMagasin.WinForms;

internal sealed class TicketPreviewForm : Form
{
    private readonly PrintDocument _document = new();
    private readonly string _ticket;
    public TicketPreviewForm(VenteHistorique sale, IReadOnlyList<LigneVenteDetail> lines)
    {
        _ticket = BuildTicket(sale, lines); Text = $"Aperçu ticket — Vente #{sale.Id}"; StartPosition = FormStartPosition.CenterParent; Width = 620; Height = 720;
        var preview = new PrintPreviewControl { Dock = DockStyle.Fill, Document = _document, Zoom = 1.0 };
        var print = new Button { Text = "Imprimer", Dock = DockStyle.Bottom, Height = 42 };
        print.Click += (_, _) => { try { using var dialog = new PrintDialog { Document = _document, UseEXDialog = true }; if (dialog.ShowDialog(this) == DialogResult.OK) _document.Print(); } catch (Exception ex) { MessageBox.Show(this, $"Impression impossible. La vente reste inchangée.\n{ex.Message}", "Impression", MessageBoxButtons.OK, MessageBoxIcon.Warning); } };
        _document.PrintPage += PrintPage; Controls.Add(preview); Controls.Add(print);
    }
    private void PrintPage(object? sender, PrintPageEventArgs e) { using var font = new Font("Consolas", 10); e.Graphics.DrawString(_ticket, font, Brushes.Black, e.MarginBounds); e.HasMorePages = false; }
    private static string BuildTicket(VenteHistorique sale, IReadOnlyList<LigneVenteDetail> lines)
    {
        var b = new StringBuilder("GESTION MAGASIN\n------------------------------\n"); b.AppendLine($"Vente : #{sale.Id}"); b.AppendLine(sale.DateVente.ToLocalTime().ToString("g")); b.AppendLine($"Vendeur : {sale.Vendeur}"); b.AppendLine($"Paiement : {sale.ModePaiement}"); b.AppendLine("------------------------------");
        foreach (var l in lines) { b.AppendLine($"{l.Reference} {l.Nom}"); b.AppendLine($"  {l.Quantite} x {l.PrixUnitaire:N2} = {l.Total:N2}"); }
        b.AppendLine("------------------------------"); b.AppendLine($"TOTAL : {sale.Total:N2}"); b.AppendLine(sale.Statut == "Annulee" ? $"ANNULÉE : {sale.MotifAnnulation}" : "Merci de votre visite"); return b.ToString();
    }
}
