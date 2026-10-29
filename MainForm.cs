using System.ComponentModel;
using System.Text.Json;

namespace PharmacyRaw;

public class MainForm : Form
{
    static readonly string DataFile = Path.Combine(AppContext.BaseDirectory, "products.json");
    static readonly string OrdersFile = Path.Combine(AppContext.BaseDirectory, "orders.txt");

    readonly BindingList<Product> products = new();
    readonly BindingList<CartItem> cart = new();

    readonly DataGridView gridProducts = NewGrid();
    readonly DataGridView gridCart = NewGrid();
    readonly DataGridView gridAdmin = NewGrid();

    readonly TextBox txtSearch = new() { Width = 200 };
    readonly NumericUpDown numQty = new() { Minimum = 1, Maximum = 100, Value = 1, Width = 60 };
    readonly Label lblTotal = new() { Text = "Итого: 0,00", AutoSize = true, Margin = new Padding(10, 8, 10, 0) };

    readonly TextBox txtName = new() { Width = 200 };
    readonly TextBox txtCategory = new() { Width = 150 };
    readonly NumericUpDown numPrice = new() { DecimalPlaces = 2, Maximum = 1000000, Width = 90 };
    private TabControl tabs;
    private TabPage tabShop;
    private TabPage tabAdmin;
    private TabPage tabPage1;
    readonly NumericUpDown numStock = new() { Maximum = 100000, Width = 70 };

    public MainForm()
    {
        Text = "Аптечная сеть 'Лирику без рецепта'";
        Width = 1000;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;

        LoadProducts();

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var tabShop = new TabPage("Каталог и корзина");
        var tabAdmin = new TabPage("Управление товарами");
        tabs.TabPages.Add(tabShop);
        tabs.TabPages.Add(tabAdmin);
        Controls.Add(tabs);

        BuildShop(tabShop);
        BuildAdmin(tabAdmin);

        ApplyFilter();
        RefreshCart();
    }

    // интерфейс 

    static DataGridView NewGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoGenerateColumns = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false
    };

    static void AddCol(DataGridView g, string prop, string header) =>
        g.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = prop, HeaderText = header });

    static Label L(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(6, 8, 0, 0) };

    void BuildShop(TabPage tab)
    {
        var right = new Panel { Dock = DockStyle.Fill };
        tab.Controls.Add(right);
        var left = new Panel { Dock = DockStyle.Left, Width = 500 };
        tab.Controls.Add(left);

        AddCol(gridProducts, "Name", "Название");
        AddCol(gridProducts, "Category", "Категория");
        AddCol(gridProducts, "Price", "Цена");
        AddCol(gridProducts, "Stock", "В наличии");
        left.Controls.Add(gridProducts);

        var topLeft = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36 };
        topLeft.Controls.Add(L("Поиск:"));
        topLeft.Controls.Add(txtSearch);
        left.Controls.Add(topLeft);
        txtSearch.TextChanged += (_, _) => ApplyFilter();

        var bottomLeft = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40 };
        var btnAdd = new Button { Text = "Добавить в корзину", AutoSize = true };
        btnAdd.Click += (_, _) => AddToCart();
        bottomLeft.Controls.Add(L("Кол-во:"));
        bottomLeft.Controls.Add(numQty);
        bottomLeft.Controls.Add(btnAdd);
        left.Controls.Add(bottomLeft);

        AddCol(gridCart, "Name", "Товар");
        AddCol(gridCart, "Price", "Цена");
        AddCol(gridCart, "Qty", "Кол-во");
        AddCol(gridCart, "Sum", "Сумма");
        gridCart.DataSource = cart;
        right.Controls.Add(gridCart);

        right.Controls.Add(new Label { Text = "КОРЗИНА", Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleCenter });

        var bottomRight = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 70 };
        var btnMinus = new Button { Text = "- 1 шт.", AutoSize = true };
        var btnRemove = new Button { Text = "Удалить позицию", AutoSize = true };
        var btnClear = new Button { Text = "Очистить", AutoSize = true };
        var btnBuy = new Button { Text = "Оформить заказ", AutoSize = true };
        btnMinus.Click += (_, _) => ChangeQty(-1);
        btnRemove.Click += (_, _) => RemoveFromCart();
        btnClear.Click += (_, _) => { cart.Clear(); RefreshCart(); };
        btnBuy.Click += (_, _) => Checkout();
        bottomRight.Controls.AddRange(new Control[] { btnMinus, btnRemove, btnClear, btnBuy, lblTotal });
        right.Controls.Add(bottomRight);
    }

    void BuildAdmin(TabPage tab)
    {
        AddCol(gridAdmin, "Id", "ID");
        AddCol(gridAdmin, "Name", "Название");
        AddCol(gridAdmin, "Category", "Категория");
        AddCol(gridAdmin, "Price", "Цена");
        AddCol(gridAdmin, "Stock", "Остаток");
        gridAdmin.DataSource = products;
        gridAdmin.SelectionChanged += (_, _) => FillFields();
        tab.Controls.Add(gridAdmin);

        var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 90 };
        var btnAdd = new Button { Text = "Добавить", AutoSize = true };
        var btnSave = new Button { Text = "Сохранить изменения", AutoSize = true };
        var btnDel = new Button { Text = "Удалить", AutoSize = true };
        btnAdd.Click += (_, _) => AddProduct();
        btnSave.Click += (_, _) => UpdateProduct();
        btnDel.Click += (_, _) => DeleteProduct();
        panel.Controls.AddRange(new Control[]
        {
            L("Название:"), txtName, L("Категория:"), txtCategory,
            L("Цена:"), numPrice, L("Остаток:"), numStock,
            btnAdd, btnSave, btnDel
        });
        tab.Controls.Add(panel);
    }

    // каталог / корзина 

    void ApplyFilter()
    {
        var q = txtSearch.Text.Trim().ToLower();
        gridProducts.DataSource = products
            .Where(p => p.Name.ToLower().Contains(q) || p.Category.ToLower().Contains(q))
            .ToList();
    }

    void AddToCart()
    {
        if (gridProducts.CurrentRow?.DataBoundItem is not Product p) return;
        int qty = (int)numQty.Value;
        var item = cart.FirstOrDefault(c => c.ProductId == p.Id);
        int already = item?.Qty ?? 0;

        if (already + qty > p.Stock)
        {
            MessageBox.Show($"Недостаточно товара на складе. Доступно: {p.Stock} шт.");
            return;
        }

        if (item == null)
            cart.Add(new CartItem { ProductId = p.Id, Name = p.Name, Price = p.Price, Qty = qty });
        else
            item.Qty += qty;

        RefreshCart();
    }

    void ChangeQty(int delta)
    {
        if (gridCart.CurrentRow?.DataBoundItem is not CartItem item) return;
        item.Qty += delta;
        if (item.Qty <= 0) cart.Remove(item);
        RefreshCart();
    }

    void RemoveFromCart()
    {
        if (gridCart.CurrentRow?.DataBoundItem is not CartItem item) return;
        cart.Remove(item);
        RefreshCart();
    }

    void RefreshCart()
    {
        cart.ResetBindings();
        lblTotal.Text = $"Итого: {cart.Sum(c => c.Sum):N2}";
    }

    void Checkout()
    {
        if (cart.Count == 0)
        {
            MessageBox.Show("Корзина пуста.");
            return;
        }

        decimal total = cart.Sum(c => c.Sum);
        if (MessageBox.Show($"Оформить заказ на сумму {total:N2}?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes)
            return;

        var lines = new List<string> { $"Заказ от {DateTime.Now:dd.MM.yyyy HH:mm}" };
        foreach (var c in cart)
        {
            var p = products.FirstOrDefault(x => x.Id == c.ProductId);
            if (p == null || p.Stock < c.Qty)
            {
                MessageBox.Show($"Товара «{c.Name}» недостаточно на складе.");
                return;
            }
        }
        foreach (var c in cart)
        {
            products.First(x => x.Id == c.ProductId).Stock -= c.Qty;
            lines.Add($"  {c.Name} x {c.Qty} = {c.Sum:N2}");
        }
        lines.Add($"  ИТОГО: {total:N2}");
        File.AppendAllLines(OrdersFile, lines);

        cart.Clear();
        SaveProducts();
        products.ResetBindings();
        ApplyFilter();
        RefreshCart();
        MessageBox.Show("Заказ оформлен. Спасибо за покупку!");
    }

    // управление товарами 

    void FillFields()
    {
        if (gridAdmin.CurrentRow?.DataBoundItem is not Product p) return;
        txtName.Text = p.Name;
        txtCategory.Text = p.Category;
        numPrice.Value = Math.Min(p.Price, numPrice.Maximum);
        numStock.Value = Math.Min(p.Stock, numStock.Maximum);
    }

    bool FieldsValid()
    {
        if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtCategory.Text) || numPrice.Value <= 0)
        {
            MessageBox.Show("Заполните название, категорию и цену (> 0).");
            return false;
        }
        return true;
    }

    void AddProduct()
    {
        if (!FieldsValid()) return;
        products.Add(new Product
        {
            Id = products.Count == 0 ? 1 : products.Max(p => p.Id) + 1,
            Name = txtName.Text.Trim(),
            Category = txtCategory.Text.Trim(),
            Price = numPrice.Value,
            Stock = (int)numStock.Value
        });
        SaveProducts();
        ApplyFilter();
    }

    void UpdateProduct()
    {
        if (gridAdmin.CurrentRow?.DataBoundItem is not Product p) return;
        if (!FieldsValid()) return;
        p.Name = txtName.Text.Trim();
        p.Category = txtCategory.Text.Trim();
        p.Price = numPrice.Value;
        p.Stock = (int)numStock.Value;
        products.ResetBindings();
        SaveProducts();
        ApplyFilter();
    }

    void DeleteProduct()
    {
        if (gridAdmin.CurrentRow?.DataBoundItem is not Product p) return;
        if (MessageBox.Show($"Удалить «{p.Name}»?", "Удаление", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        products.Remove(p);
        var inCart = cart.FirstOrDefault(c => c.ProductId == p.Id);
        if (inCart != null) cart.Remove(inCart);
        SaveProducts();
        ApplyFilter();
        RefreshCart();
    }

    // хранение 

    void LoadProducts()
    {
        List<Product> list = null;
        try
        {
            if (File.Exists(DataFile))
                list = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(DataFile));
        }
        catch { /* битый файл - создадим заново */ }

        list ??= new List<Product>
        {
            new() { Id = 1, Name = "Парацетамол 500 мг", Category = "Жаропонижающие", Price = 45.50m, Stock = 100 },
            new() { Id = 2, Name = "Ибупрофен 200 мг", Category = "Обезболивающие", Price = 120m, Stock = 60 },
            new() { Id = 3, Name = "Активированный уголь", Category = "Желудок и кишечник", Price = 35m, Stock = 150 },
            new() { Id = 4, Name = "Витамин C 1000 мг", Category = "Витамины", Price = 310m, Stock = 40 },
            new() { Id = 5, Name = "Нурофен сироп", Category = "Детские", Price = 285m, Stock = 25 },
            new() { Id = 6, Name = "Пластырь бактерицидный", Category = "Перевязочные", Price = 28m, Stock = 200 },
            new() { Id = 7, Name = "Аскорбиновая кислота", Category = "Витамины", Price = 20m, Stock = 120 },
            new() { Id = 8, Name = "Но-шпа 40 мг", Category = "Спазмолитики", Price = 150m, Stock = 80 },
        };

        foreach (var p in list) products.Add(p);
        if (!File.Exists(DataFile)) SaveProducts();
    }

    private void InitializeComponent()
    {

    }

    void SaveProducts()
    {
        File.WriteAllText(DataFile, JsonSerializer.Serialize(products.ToList(),
            new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }
}
