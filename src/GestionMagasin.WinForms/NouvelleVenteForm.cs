namespace GestionMagasin.WinForms;

using GestionMagasin.Models;
using GestionMagasin.Services;

/// <summary>Panier et validation d'une vente. Les tarifs et le stock sont revérifiés dans SQLite au commit.</summary>
internal sealed class NouvelleVenteForm : Form
{
    private readonly CatalogueService _catalogue;
    private readonly VenteService _ventes;
    private readonly TextBox _search = new() { PlaceholderText = "Rechercher référence ou produit…", Dock = DockStyle.Fill };
    private readonly DataGridView _results = Grid();
    private readonly DataGridView _cartGrid = Grid();
    private readonly BindingSource _cartSource = new();
    private readonly List<CartLine> _cart = [];
    private readonly ComboBox _payment = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly NumericUpDown _received = new() { DecimalPlaces = 2, Minimum = 0, Maximum = 100000000, ThousandsSeparator = true, Width = 160 };
    private readonly Label _totalLabel = new() { AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
    private readonly Label _changeLabel = new() { AutoSize = true };
    private readonly Button _validate = new() { Text = "Valider la vente", AutoSize = true };
    private readonly string _token = Guid.NewGuid().ToString("N");

    public NouvelleVenteForm(CatalogueService catalogue, VenteService ventes)
    {
        _catalogue = catalogue;
        _ventes = ventes;
        Text = "Nouvelle vente"; StartPosition = FormStartPosition.CenterParent; WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(900, 600);
        BuildUi();
        _search.TextChanged += (_, _) => RefreshSearch();
        _payment.SelectedIndexChanged += (_, _) => UpdateTotals();
        _received.ValueChanged += (_, _) => UpdateTotals();
        _validate.Click += ValidateSale;
        RefreshSearch(); UpdateTotals();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.Controls.Add(_search, 0, 0); ConfigureResults(); left.Controls.Add(_results, 0, 1);
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 110)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        ConfigureCart(); _cartGrid.DataSource = _cartSource;
        var cartActions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        cartActions.Controls.Add(Button("Ajouter sélection", (_, _) => AddSelected()));
        cartActions.Controls.Add(Button("Modifier quantité", (_, _) => ChangeQuantity()));
        cartActions.Controls.Add(Button("Retirer ligne", (_, _) => RemoveSelected()));
        var paymentPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        _payment.Items.AddRange(["Espèces", "Mobile Money", "Carte"]); _payment.SelectedIndex = 0;
        var paymentRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        paymentRow.Controls.Add(new Label { Text = "Paiement", AutoSize = true, Padding = new Padding(0, 7, 8, 0) }); paymentRow.Controls.Add(_payment);
        paymentRow.Controls.Add(new Label { Text = "Montant reçu", AutoSize = true, Padding = new Padding(12, 7, 8, 0) }); paymentRow.Controls.Add(_received);
        paymentPanel.Controls.Add(paymentRow); paymentPanel.Controls.Add(_totalLabel); paymentPanel.Controls.Add(_changeLabel);
        var validatePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var cancel = Button("Fermer", (_, _) => Close()); validatePanel.Controls.Add(cancel); validatePanel.Controls.Add(_validate);
        right.Controls.Add(_cartGrid, 0, 0); right.Controls.Add(cartActions, 0, 1); right.Controls.Add(paymentPanel, 0, 2); right.Controls.Add(validatePanel, 0, 3);
        root.Controls.Add(left, 0, 0); root.Controls.Add(right, 1, 0); Controls.Add(root);
    }

    private void ConfigureResults()
    {
        _results.AutoGenerateColumns = false;
        _results.Columns.Add(Column(nameof(Produit.Reference), "Référence"));
        _results.Columns.Add(Column(nameof(Produit.Nom), "Produit"));
        _results.Columns.Add(Column(nameof(Produit.PrixVente), "Prix", "N2"));
        _results.Columns.Add(Column(nameof(Produit.Stock), "Stock"));
        _results.CellDoubleClick += (_, _) => AddSelected();
    }

    private void ConfigureCart()
    {
        _cartGrid.AutoGenerateColumns = false;
        _cartGrid.Columns.Add(Column(nameof(CartLine.Reference), "Référence"));
        _cartGrid.Columns.Add(Column(nameof(CartLine.Nom), "Produit"));
        _cartGrid.Columns.Add(Column(nameof(CartLine.PrixUnitaire), "Prix", "N2"));
        _cartGrid.Columns.Add(Column(nameof(CartLine.Quantite), "Qté"));
        _cartGrid.Columns.Add(Column(nameof(CartLine.Total), "Total", "N2"));
    }

    private static DataGridView Grid() => new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false };
    private static DataGridViewTextBoxColumn Column(string property, string header, string? format = null) => new() { DataPropertyName = property, HeaderText = header, SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = new DataGridViewCellStyle { Format = format ?? "" } };
    private static Button Button(string text, EventHandler action) { var button = new Button { Text = text, AutoSize = true }; button.Click += action; return button; }

    private void RefreshSearch()
    {
        try { _results.DataSource = _catalogue.Rechercher(_search.Text, null, true).Where(p => p.Stock > 0).ToList(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Recherche impossible", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void AddSelected()
    {
        if (_results.CurrentRow?.DataBoundItem is not Produit product) return;
        var existing = _cart.SingleOrDefault(x => x.ProduitId == product.Id);
        if (existing is null) _cart.Add(new CartLine(product.Id, product.Reference, product.Nom, product.PrixVente, 1));
        else existing.Quantite = checked(existing.Quantite + 1);
        RefreshCart();
    }

    private void ChangeQuantity()
    {
        if (_cartGrid.CurrentRow?.DataBoundItem is not CartLine line) return;
        using var prompt = new QuantityForm(line.Quantite);
        if (prompt.ShowDialog(this) != DialogResult.OK) return;
        if (prompt.Quantity == 0) _cart.Remove(line); else line.Quantite = prompt.Quantity;
        RefreshCart();
    }

    private void RemoveSelected()
    {
        if (_cartGrid.CurrentRow?.DataBoundItem is CartLine line) { _cart.Remove(line); RefreshCart(); }
    }

    private void RefreshCart() { _cartSource.DataSource = null; _cartSource.DataSource = _cart.ToList(); UpdateTotals(); }

    private decimal Total => _cart.Aggregate(0m, (sum, line) => checked(sum + checked(line.PrixUnitaire * line.Quantite)));

    private void UpdateTotals()
    {
        var total = Total;
        _totalLabel.Text = $"Total : {total:N2}";
        var cash = _payment.SelectedIndex == 0;
        _received.Enabled = cash;
        if (!cash) _received.Value = Math.Min(_received.Maximum, total);
        var change = cash ? _received.Value - total : 0;
        _changeLabel.Text = cash ? $"Monnaie à rendre : {Math.Max(0, change):N2}" : "Monnaie à rendre : 0,00";
        _validate.Enabled = _cart.Count > 0 && (!cash || _received.Value >= total);
    }

    private void ValidateSale(object? sender, EventArgs e)
    {
        if (_cart.Count == 0) return;
        _validate.Enabled = false;
        try
        {
            var method = _payment.SelectedIndex switch { 0 => ModePaiement.Especes, 1 => ModePaiement.MobileMoney, _ => ModePaiement.Carte };
            var lines = _cart.Select(x => new LigneVenteDemandee(x.ProduitId, x.Quantite, x.PrixUnitaire)).ToList();
            var sale = _ventes.Valider(_token, lines, method, _received.Value);
            MessageBox.Show(this, $"Vente n° {sale.Id} validée.\nTotal : {sale.Total:N2}\nMonnaie rendue : {sale.MonnaieRendue:N2}", "Vente enregistrée", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Vente non validée", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _validate.Enabled = _cart.Count > 0 && (_payment.SelectedIndex != 0 || _received.Value >= Total);
            RefreshSearch();
        }
    }

    private sealed class CartLine(long produitId, string reference, string nom, decimal prixUnitaire, long quantite)
    {
        public long ProduitId { get; } = produitId;
        public string Reference { get; } = reference;
        public string Nom { get; } = nom;
        public decimal PrixUnitaire { get; } = prixUnitaire;
        public long Quantite { get; set; } = quantite;
        public decimal Total => checked(PrixUnitaire * Quantite);
    }

    private sealed class QuantityForm : Form
    {
        private readonly NumericUpDown _value = new() { Minimum = 0, Maximum = 100000000, ThousandsSeparator = true };
        public long Quantity => (long)_value.Value;
        public QuantityForm(long current)
        {
            Text = "Quantité (0 pour retirer)"; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; ClientSize = new Size(300, 120); MaximizeBox = false; MinimizeBox = false;
            _value.Value = Math.Min(_value.Maximum, current); _value.Dock = DockStyle.Top;
            var ok = new Button { Text = "Appliquer", Dock = DockStyle.Bottom, DialogResult = DialogResult.OK };
            Controls.Add(_value); Controls.Add(ok); AcceptButton = ok;
        }
    }
}
