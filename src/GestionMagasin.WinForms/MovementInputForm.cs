namespace GestionMagasin.WinForms;

internal sealed class MovementInputForm : Form
{
    private readonly NumericUpDown _quantity = new() { Minimum = 0, Maximum = 1000000000, ThousandsSeparator = true };
    private readonly TextBox _reason = new();
    public long Quantite => (long)_quantity.Value;
    public string Motif => _reason.Text.Trim();

    public MovementInputForm(string title, string quantityLabel, string reasonLabel)
    {
        Text = title; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ClientSize = new Size(440, 190);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 3 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.Controls.Add(new Label { Text = quantityLabel, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0); _quantity.Dock = DockStyle.Fill; table.Controls.Add(_quantity, 1, 0);
        table.Controls.Add(new Label { Text = reasonLabel, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1); _reason.Dock = DockStyle.Fill; table.Controls.Add(_reason, 1, 1);
        var save = new Button { Text = "Valider", AutoSize = true, Anchor = AnchorStyles.Right }; save.Click += (_, _) => { if (Quantite <= 0 || string.IsNullOrWhiteSpace(Motif)) { MessageBox.Show(this, "Quantité positive et motif requis."); return; } DialogResult = DialogResult.OK; Close(); };
        table.Controls.Add(save, 1, 2); AcceptButton = save; Controls.Add(table);
    }
}
