namespace GestionMagasin.Services;

public sealed class ProduitForm : Form
{
    private readonly TextBox _reference = new(); private readonly TextBox _name = new();
    private readonly ComboBox _category = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _price = new() { DecimalPlaces = 2, Maximum = 100000000, Minimum = 0, ThousandsSeparator = true };
    private readonly NumericUpDown _threshold = new() { Maximum = 1000000, Minimum = 0 };
    public string Reference => _reference.Text.Trim(); public string Nom => _name.Text.Trim();
    public long CategorieId => ((GestionMagasin.Models.Categorie)_category.SelectedItem!).Id;
    public decimal Prix => _price.Value; public int Seuil => (int)_threshold.Value;

    public ProduitForm(IReadOnlyList<GestionMagasin.Models.Categorie> categories, GestionMagasin.Models.Produit? product = null)
    {
        Text = product is null ? "Nouveau produit" : "Modifier le produit"; StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ClientSize = new Size(430, 280);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 6 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Add(table, "Référence", _reference, 0); Add(table, "Nom", _name, 1);
        _category.DataSource = categories.ToList(); _category.DisplayMember = nameof(GestionMagasin.Models.Categorie.Nom); _category.ValueMember = nameof(GestionMagasin.Models.Categorie.Id);
        Add(table, "Catégorie", _category, 2); Add(table, "Prix de vente", _price, 3); Add(table, "Seuil d'alerte", _threshold, 4);
        var save = new Button { Text = "Enregistrer", AutoSize = true, Anchor = AnchorStyles.Right };
        save.Click += (_, _) => { if (_category.SelectedItem is null) { MessageBox.Show(this, "Créez ou sélectionnez une catégorie."); return; } DialogResult = DialogResult.OK; Close(); };
        table.Controls.Add(save, 1, 5); AcceptButton = save; Controls.Add(table);
        if (product is not null) { _reference.Text = product.Reference; _name.Text = product.Nom; _category.SelectedValue = product.CategorieId; _price.Value = product.PrixVente; _threshold.Value = product.SeuilAlerte; }
    }
    private static void Add(TableLayoutPanel table, string label, Control control, int row)
    { table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row); control.Dock = DockStyle.Fill; table.Controls.Add(control, 1, row); }
}
