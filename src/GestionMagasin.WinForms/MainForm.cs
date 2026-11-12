using GestionMagasin.Models;
using GestionMagasin.Services;

namespace GestionMagasin.WinForms;

internal sealed class MainForm : Form
{
    private readonly CatalogueService _catalogue;
    private readonly StockService _stock;
    private readonly DataGridView _products = Grid();
    private readonly DataGridView _movements = Grid();
    private readonly TextBox _search = new() { PlaceholderText = "Rechercher par référence ou nom…", Width = 260 };
    private readonly ComboBox _category = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly ComboBox _status = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly Label _databaseInfo;

    public MainForm(string databasePath, CatalogueService catalogue, StockService stock)
    {
        _catalogue = catalogue; _stock = stock; _databaseInfo = new Label { Text = $"Base locale : {databasePath}", AutoSize = true, ForeColor = Color.DimGray };
        Text = "Gestion Magasin — Produits et stocks"; StartPosition = FormStartPosition.CenterScreen; WindowState = FormWindowState.Maximized;
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildProductsPage()); tabs.TabPages.Add(BuildStockPage());
        Controls.Add(tabs); LoadCategories(); RefreshProducts(); RefreshMovements();
    }

    private TabPage BuildProductsPage()
    {
        var page = new TabPage("Produits"); var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 3, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        _category.SelectedIndexChanged += (_, _) => RefreshProducts(); _status.SelectedIndexChanged += (_, _) => RefreshProducts();
        _search.TextChanged += (_, _) => RefreshProducts();
        _status.Items.AddRange(["Tous", "Actifs", "Archivés"]); _status.SelectedIndex = 0;
        toolbar.Controls.AddRange([_search, _category, _status, Button("Nouveau", (_, _) => AddProduct()), Button("Modifier", (_, _) => EditProduct()), Button("Archiver / réactiver", (_, _) => ToggleProduct()), Button("Nouvelle catégorie", (_, _) => AddCategory())]);
        panel.Controls.Add(toolbar, 0, 0); ConfigureProductGrid(); panel.Controls.Add(_products, 0, 1); panel.Controls.Add(_databaseInfo, 0, 2); page.Controls.Add(panel); return page;
    }

    private TabPage BuildStockPage()
    {
        var page = new TabPage("Stocks & mouvements"); var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        toolbar.Controls.AddRange([Button("Stock initial", (_, _) => AddMovement(true)), Button("Réapprovisionner", (_, _) => AddMovement(false)), Button("Correction inventaire", (_, _) => CorrectStock()), Button("Actualiser", (_, _) => { RefreshProducts(); RefreshMovements(); })]);
        panel.Controls.Add(toolbar, 0, 0); _movements.AutoGenerateColumns = true; _movements.DataSource = new BindingSource { DataSource = new List<MouvementStock>() }; panel.Controls.Add(_movements, 0, 1); page.Controls.Add(panel); return page;
    }

    private static DataGridView Grid() => new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false, AutoGenerateColumns = true };

    private void ConfigureProductGrid()
    {
        _products.AutoGenerateColumns = false;
        _products.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Produit.Reference), HeaderText = "Référence", SortMode = DataGridViewColumnSortMode.Automatic });
        _products.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Produit.Nom), HeaderText = "Produit", SortMode = DataGridViewColumnSortMode.Automatic });
        _products.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Produit.CategorieNom), HeaderText = "Catégorie", SortMode = DataGridViewColumnSortMode.Automatic });
        _products.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Produit.PrixVente), HeaderText = "Prix", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _products.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Produit.Stock), HeaderText = "Stock", SortMode = DataGridViewColumnSortMode.Automatic });
        _products.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Produit.SeuilAlerte), HeaderText = "Seuil" });
        _products.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Produit.StatutStock), HeaderText = "Disponibilité" });
        _products.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(Produit.Actif), HeaderText = "Actif" });
        _products.CellFormatting += (_, e) =>
        {
            if (_products.Rows[e.RowIndex].DataBoundItem is not Produit product) return;
            var alert = ReglesStock.Evaluer(product.Stock, product.SeuilAlerte);
            _products.Rows[e.RowIndex].DefaultCellStyle.ForeColor = alert.Couleur switch { "Red" => Color.Firebrick, "DarkOrange" => Color.DarkOrange, _ => Color.DarkGreen };
            if (!product.Actif) _products.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Gainsboro;
        };
    }

    private void LoadCategories()
    {
        var categories = _catalogue.Categories().ToList(); categories.Insert(0, new Categorie(0, "Toutes les catégories"));
        _category.DataSource = categories; _category.DisplayMember = nameof(Categorie.Nom); _category.ValueMember = nameof(Categorie.Id); _category.SelectedIndex = 0;
    }
    private void RefreshProducts()
    {
        if (IsDisposed || _category.SelectedItem is null || _status.SelectedItem is null) return;
        long? category = Convert.ToInt64(_category.SelectedValue) == 0 ? null : Convert.ToInt64(_category.SelectedValue);
        bool? active = _status.SelectedIndex == 1 ? true : _status.SelectedIndex == 2 ? false : null;
        _products.DataSource = _catalogue.Rechercher(_search.Text, category, active).ToList();
    }
    private void RefreshMovements() => _movements.DataSource = _stock.Mouvements().ToList();
    private Produit? SelectedProduct() => _products.CurrentRow?.DataBoundItem as Produit;
    private static Button Button(string text, EventHandler action) { var button = new Button { Text = text, AutoSize = true }; button.Click += action; return button; }

    private void AddProduct()
    {
        using var form = new ProduitForm(_catalogue.Categories()); if (form.ShowDialog(this) != DialogResult.OK) return;
        Run(() => _catalogue.CreerProduit(form.Reference, form.Nom, form.CategorieId, form.Prix, form.Seuil));
    }
    private void EditProduct()
    {
        var product = SelectedProduct(); if (product is null) { Warn("Sélectionnez un produit."); return; }
        using var form = new ProduitForm(_catalogue.Categories(), product); if (form.ShowDialog(this) != DialogResult.OK) return;
        Run(() => _catalogue.ModifierProduit(product.Id, form.Reference, form.Nom, form.CategorieId, form.Prix, form.Seuil));
    }
    private void ToggleProduct()
    {
        var product = SelectedProduct(); if (product is null) { Warn("Sélectionnez un produit."); return; }
        Run(() => { if (product.Actif) _catalogue.Archiver(product.Id); else _catalogue.Reactiver(product.Id); });
    }
    private void AddCategory()
    {
        var name = Microsoft.VisualBasic.Interaction.InputBox("Nom de la nouvelle catégorie :", "Nouvelle catégorie");
        if (!string.IsNullOrWhiteSpace(name)) Run(() => _catalogue.CreerCategorie(name), LoadCategories);
    }
    private void AddMovement(bool initial)
    {
        var product = SelectedProduct(); if (product is null) { Warn("Sélectionnez d'abord le produit dans l'onglet Produits."); return; }
        using var form = new MovementInputForm(initial ? "Stock initial" : "Réapprovisionnement", "Quantité à ajouter", "Motif");
        if (form.ShowDialog(this) != DialogResult.OK) return;
        Run(() => initial ? _stock.Initialiser(product.Id, form.Quantite, form.Motif) : _stock.Reapprovisionner(product.Id, form.Quantite, form.Motif), RefreshMovements);
    }
    private void CorrectStock()
    {
        var product = SelectedProduct(); if (product is null) { Warn("Sélectionnez d'abord le produit dans l'onglet Produits."); return; }
        using var form = new MovementInputForm("Correction inventaire", "Quantité réellement comptée", "Motif");
        if (form.ShowDialog(this) != DialogResult.OK) return;
        Run(() => _stock.Corriger(product.Id, form.Quantite, form.Motif), RefreshMovements);
    }
    private void Run(Action operation, Action? after = null)
    {
        try { operation(); RefreshProducts(); RefreshMovements(); after?.Invoke(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Opération impossible", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private void Warn(string message) => MessageBox.Show(this, message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
