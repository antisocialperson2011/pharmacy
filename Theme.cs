namespace PharmacyPro;

// Единое оформление: цвета, шрифты, кнопки, таблицы
public static class Theme
{
    public static readonly Color Primary = Color.FromArgb(46, 125, 50);
    public static readonly Color PrimaryDark = Color.FromArgb(27, 94, 32);
    public static readonly Color Danger = Color.FromArgb(198, 40, 40);
    public static readonly Color Bg = Color.FromArgb(244, 248, 245);
    public static readonly Color TextColor = Color.FromArgb(33, 37, 41);

    public static readonly Font Base = new("Segoe UI", 10f);
    public static readonly Font Bold = new("Segoe UI Semibold", 10f);
    public static readonly Font Title = new("Segoe UI Semibold", 16f);

    public static Button Btn(string text, Color? color = null, int width = 140)
    {
        var b = new Button
        {
            Text = text,
            Width = width,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = color ?? Primary,
            ForeColor = Color.White,
            Font = Bold,
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 3, 3, 3),
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    public static Label Lbl(string text) =>
        new() { Text = text, AutoSize = true, Margin = new Padding(8, 8, 4, 0) };

    // Подпись над полем ввода (для форм с ручной расстановкой)
    public static void Place(Control parent, string label, Control c, int x, int y, int w)
    {
        parent.Controls.Add(new Label { Text = label, Left = x, Top = y, AutoSize = true });
        c.Left = x;
        c.Top = y + 24;
        c.Width = w;
        parent.Controls.Add(c);
    }

    public static DataGridView Grid()
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(226, 232, 228),
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 38,
            Font = Base
        };
        g.RowTemplate.Height = 30;
        g.ColumnHeadersDefaultCellStyle.BackColor = Primary;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.Font = Bold;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primary;
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 230, 201);
        g.DefaultCellStyle.SelectionForeColor = TextColor;
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 251, 248);
        return g;
    }

    public static void Col(DataGridView g, string prop, string header, float weight = 1, string format = null)
    {
        var col = new DataGridViewTextBoxColumn { DataPropertyName = prop, HeaderText = header, FillWeight = weight * 100 };
        if (format != null) col.DefaultCellStyle.Format = format;
        g.Columns.Add(col);
    }
}

// Базовая форма рабочих окон (клиент / админ): шапка с кнопкой "Выйти"
public abstract class ShellForm : Form
{
    public bool LoggedOut { get; private set; }

    protected Panel BuildHeader(string userText)
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Theme.Primary };
        var title = new Label
        {
            Text = "✚  Аптека «Лирика»",
            ForeColor = Color.White,
            Font = Theme.Title,
            AutoSize = true,
            Left = 16,
            Top = 12
        };
        var user = new Label
        {
            Text = userText,
            ForeColor = Color.White,
            Dock = DockStyle.Right,
            Width = 320,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0)
        };
        var btn = Theme.Btn("Выйти", Theme.PrimaryDark, 90);
        btn.Dock = DockStyle.Right;
        btn.Margin = Padding.Empty;
        btn.Click += (_, _) =>
        {
            if (MessageBox.Show("Выйти из аккаунта?", "Выход", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                LoggedOut = true;
                Close();
            }
        };
        p.Controls.Add(title);
        p.Controls.Add(user);
        p.Controls.Add(btn);
        return p;
    }
}
