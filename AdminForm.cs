using System.ComponentModel;

namespace PharmacyPro;

public class AdminForm : ShellForm
{
    readonly User admin;
    readonly BindingList<Product> products = new(Db.Products);
    readonly BindingList<User> users = new(Db.Users);

    readonly DataGridView gProducts = Theme.Grid();
    readonly DataGridView gOrders = Theme.Grid();
    readonly DataGridView gLines = Theme.Grid();
    readonly DataGridView gUsers = Theme.Grid();

    // поля редактирования товара
    readonly TextBox txtName = new();
    readonly ComboBox cbCategory = new();
    readonly NumericUpDown numPrice = new() { DecimalPlaces = 2, Maximum = 1000000 };
    readonly NumericUpDown numStock = new() { Maximum = 100000 };
    readonly NumericUpDown numPercent = new() { Minimum = -90, Maximum = 500, Value = 10, Width = 70 };

    readonly ComboBox cbStatus = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    readonly Label lblStats = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 11f),
        ForeColor = Theme.Primary,
        Margin = new Padding(25, 8, 0, 0)
    };

    readonly TabControl tabs = new() { Dock = DockStyle.Fill, Font = Theme.Base };
    readonly TabPage tabProducts = new("Товары и цены") { BackColor = Theme.Bg };
    readonly TabPage tabOrders = new("Заказы") { BackColor = Theme.Bg };
    readonly TabPage tabUsers = new("Пользователи") { BackColor = Theme.Bg };

    public AdminForm(User u)
    {
        admin = u;
        Text = "Аптека «Здоровье» — панель администратора";
        Font = Theme.Base;
        BackColor = Theme.Bg;
        Width = 1050;
        Height = 700;
        MinimumSize = new Size(950, 600);
        StartPosition = FormStartPosition.CenterScreen;

        tabs.TabPages.AddRange(new[] { tabProducts, tabOrders, tabUsers });
        Controls.Add(tabs);
        Controls.Add(BuildHeader($"Администратор: {u.FullName}"));

        BuildProducts();
        BuildOrders();
        BuildUsers();
        LoadOrders();
    }

    // ---------- товары ----------
    void BuildProducts()
    {
        Theme.Col(gProducts, "Id", "ID", 0.4f);
        Theme.Col(gProducts, "Name", "Название", 3);
        Theme.Col(gProducts, "Category", "Категория", 1.5f);
        Theme.Col(gProducts, "Price", "Цена, ₽", 1, "N2");
        Theme.Col(gProducts, "Stock", "Остаток", 1);
        gProducts.DataSource = products;
        gProducts.SelectionChanged += (_, _) => FillFields();
        tabProducts.Controls.Add(gProducts);

        var edit = new Panel { Dock = DockStyle.Bottom, Height = 170, BackColor = Color.White };
        Theme.Place(edit, "Название", txtName, 12, 10, 280);
        Theme.Place(edit, "Категория", cbCategory, 305, 10, 190);
        Theme.Place(edit, "Цена, ₽", numPrice, 508, 10, 110);
        Theme.Place(edit, "Остаток, шт.", numStock, 631, 10, 100);
        RefreshCategories();

        var bAdd = Theme.Btn("Добавить");
        var bSave = Theme.Btn("Сохранить изменения", null, 200);
        var bDel = Theme.Btn("Удалить", Theme.Danger);
        bAdd.Left = 12; bAdd.Top = 70;
        bSave.Left = 160; bSave.Top = 70;
        bDel.Left = 370; bDel.Top = 70;
        bAdd.Click += (_, _) => AddProduct();
        bSave.Click += (_, _) => UpdateProduct();
        bDel.Click += (_, _) => DeleteProduct();
        edit.Controls.AddRange(new Control[] { bAdd, bSave, bDel });

        var lblP = new Label { Text = "Изменить все цены на, %:", AutoSize = true, Left = 12, Top = 125 };
        numPercent.Left = 215; numPercent.Top = 121;
        var bPercent = Theme.Btn("Применить", Theme.PrimaryDark, 120);
        bPercent.Left = 300; bPercent.Top = 117; bPercent.Height = 34;
        bPercent.Click += (_, _) => ChangeAllPrices();
        edit.Controls.AddRange(new Control[] { lblP, numPercent, bPercent });

        tabProducts.Controls.Add(edit);
    }

    void RefreshCategories()
    {
        var current = cbCategory.Text;
        cbCategory.Items.Clear();
        cbCategory.Items.AddRange(Db.Products.Select(p => p.Category).Distinct().OrderBy(x => x).ToArray());
        cbCategory.Text = current;
    }

    void FillFields()
    {
        if (gProducts.CurrentRow?.DataBoundItem is not Product p) return;
        txtName.Text = p.Name;
        cbCategory.Text = p.Category;
        numPrice.Value = Math.Min(p.Price, numPrice.Maximum);
        numStock.Value = Math.Min(p.Stock, numStock.Maximum);
    }

    bool FieldsValid()
    {
        if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(cbCategory.Text) || numPrice.Value <= 0)
        {
            MessageBox.Show("Заполните название, категорию и цену (больше 0).", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }

    void AddProduct()
    {
        if (!FieldsValid()) return;
        products.Add(new Product
        {
            Id = Db.NextProductId(),
            Name = txtName.Text.Trim(),
            Category = cbCategory.Text.Trim(),
            Price = numPrice.Value,
            Stock = (int)numStock.Value
        });
        Db.Save();
        RefreshCategories();
    }

    void UpdateProduct()
    {
        if (gProducts.CurrentRow?.DataBoundItem is not Product p) return;
        if (!FieldsValid()) return;
        p.Name = txtName.Text.Trim();
        p.Category = cbCategory.Text.Trim();
        p.Price = numPrice.Value;
        p.Stock = (int)numStock.Value;
        products.ResetBindings();
        Db.Save();
        RefreshCategories();
    }

    void DeleteProduct()
    {
        if (gProducts.CurrentRow?.DataBoundItem is not Product p) return;
        if (MessageBox.Show($"Удалить товар «{p.Name}»?", "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        products.Remove(p);
        Db.Save();
        RefreshCategories();
    }

    void ChangeAllPrices()
    {
        decimal k = 1 + numPercent.Value / 100m;
        if (MessageBox.Show($"Изменить цены всех товаров на {numPercent.Value}%?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        foreach (var p in Db.Products)
            p.Price = Math.Round(p.Price * k, 2);
        products.ResetBindings();
        Db.Save();
        FillFields();
    }

    // ---------- заказы ----------
    void BuildOrders()
    {
        Theme.Col(gLines, "Name", "Товар", 3);
        Theme.Col(gLines, "Price", "Цена, ₽", 1, "N2");
        Theme.Col(gLines, "Qty", "Кол-во", 1);
        Theme.Col(gLines, "Sum", "Сумма, ₽", 1, "N2");
        tabOrders.Controls.Add(gLines);

        Theme.Col(gOrders, "Id", "№", 0.5f);
        Theme.Col(gOrders, "Date", "Дата", 1.3f, "dd.MM.yyyy HH:mm");
        Theme.Col(gOrders, "UserLogin", "Клиент", 1.2f);
        Theme.Col(gOrders, "Branch", "Аптека", 2.5f);
        Theme.Col(gOrders, "Payment", "Оплата", 1.5f);
        Theme.Col(gOrders, "Status", "Статус", 1);
        Theme.Col(gOrders, "Total", "Сумма, ₽", 1, "N2");
        gOrders.Dock = DockStyle.Top;
        gOrders.Height = 250;
        gOrders.SelectionChanged += (_, _) =>
            gLines.DataSource = (gOrders.CurrentRow?.DataBoundItem as Order)?.Lines;
        tabOrders.Controls.Add(gOrders);

        cbStatus.Items.AddRange(Db.Statuses);
        cbStatus.SelectedIndex = 0;
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(4, 8, 0, 0) };
        var bStatus = Theme.Btn("Сменить статус", null, 160);
        bStatus.Click += (_, _) => ChangeStatus();
        bottom.Controls.AddRange(new Control[] { Theme.Lbl("Новый статус:"), cbStatus, bStatus, lblStats });
        tabOrders.Controls.Add(bottom);
    }

    void LoadOrders()
    {
        gOrders.DataSource = Db.Orders.OrderByDescending(o => o.Date).ToList();
        var valid = Db.Orders.Where(o => o.Status != "Отменён").ToList();
        lblStats.Text = $"Заказов: {valid.Count}   |   Выручка: {valid.Sum(o => o.Total):N2} ₽";
    }

    void ChangeStatus()
    {
        if (gOrders.CurrentRow?.DataBoundItem is not Order o) return;
        var s = cbStatus.SelectedItem as string;
        if (s == null || s == o.Status) return;
        if (o.Status == "Отменён")
        {
            MessageBox.Show("Отменённый заказ изменить нельзя.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (s == "Отменён")
        {
            Db.Cancel(o); // вернёт товар на склад
            products.ResetBindings();
        }
        else
        {
            o.Status = s;
            Db.Save();
        }
        LoadOrders();
    }

    // ---------- пользователи ----------
    void BuildUsers()
    {
        Theme.Col(gUsers, "Login", "Логин", 1.5f);
        Theme.Col(gUsers, "FullName", "Имя", 2);
        Theme.Col(gUsers, "RoleText", "Роль", 1.5f);
        gUsers.DataSource = users;
        tabUsers.Controls.Add(gUsers);

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(4, 8, 0, 0) };
        var bRole = Theme.Btn("Сменить роль", null, 160);
        var bDel = Theme.Btn("Удалить", Theme.Danger);
        bRole.Click += (_, _) =>
        {
            if (gUsers.CurrentRow?.DataBoundItem is not User u) return;
            if (u.Login == admin.Login) { MessageBox.Show("Свою роль менять нельзя."); return; }
            u.Role = u.Role == Role.Admin ? Role.Client : Role.Admin;
            users.ResetBindings();
            Db.Save();
        };
        bDel.Click += (_, _) =>
        {
            if (gUsers.CurrentRow?.DataBoundItem is not User u) return;
            if (u.Login == admin.Login) { MessageBox.Show("Нельзя удалить самого себя."); return; }
            if (MessageBox.Show($"Удалить пользователя {u.Login}?", "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            users.Remove(u);
            Db.Save();
        };
        bottom.Controls.AddRange(new Control[] { bRole, bDel });
        tabUsers.Controls.Add(bottom);
    }
}
