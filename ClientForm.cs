using System.ComponentModel;

namespace PharmacyPro;

public class ClientForm : ShellForm
{
    readonly User user;
    readonly BindingList<CartItem> cart = new();

    readonly DataGridView gCatalog = Theme.Grid();
    readonly DataGridView gCart = Theme.Grid();
    readonly DataGridView gOrders = Theme.Grid();
    readonly DataGridView gLines = Theme.Grid();

    readonly TextBox txtSearch = new() { Width = 220 };
    readonly ComboBox cbCat = new() { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly NumericUpDown numQty = new() { Minimum = 1, Maximum = 100, Value = 1, Width = 70 };
    readonly ComboBox cbBranch = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly ComboBox cbPay = new() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly Label lblTotal = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 13f),
        ForeColor = Theme.Primary,
        Margin = new Padding(20, 6, 10, 0)
    };

    readonly TabControl tabs = new() { Dock = DockStyle.Fill, Font = Theme.Base };
    readonly TabPage tabCatalog = new("Каталог") { BackColor = Theme.Bg };
    readonly TabPage tabCart = new("Корзина") { BackColor = Theme.Bg };
    readonly TabPage tabOrders = new("Мои заказы") { BackColor = Theme.Bg };

    public ClientForm(User u)
    {
        user = u;
        Text = "Аптека «Здоровье» — личный кабинет";
        Font = Theme.Base;
        BackColor = Theme.Bg;
        Width = 1000;
        Height = 650;
        MinimumSize = new Size(900, 550);
        StartPosition = FormStartPosition.CenterScreen;

        tabs.TabPages.AddRange(new[] { tabCatalog, tabCart, tabOrders });
        Controls.Add(tabs);
        Controls.Add(BuildHeader($"Здравствуйте, {u.FullName}"));

        BuildCatalog();
        BuildCart();
        BuildOrders();

        ApplyFilter();
        RefreshCart();
        LoadOrders();
    }

    // ---------- каталог ----------
    void BuildCatalog()
    {
        Theme.Col(gCatalog, "Name", "Название", 3);
        Theme.Col(gCatalog, "Category", "Категория", 1.5f);
        Theme.Col(gCatalog, "Price", "Цена, ₽", 1, "N2");
        Theme.Col(gCatalog, "Stock", "В наличии", 1);
        tabCatalog.Controls.Add(gCatalog);

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(4, 8, 0, 0) };
        top.Controls.AddRange(new Control[] { Theme.Lbl("Поиск:"), txtSearch, Theme.Lbl("Категория:"), cbCat });
        tabCatalog.Controls.Add(top);

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(4, 8, 0, 0) };
        var btnAdd = Theme.Btn("В корзину");
        btnAdd.Click += (_, _) => AddToCart();
        bottom.Controls.AddRange(new Control[] { Theme.Lbl("Количество:"), numQty, btnAdd });
        tabCatalog.Controls.Add(bottom);

        cbCat.Items.Add("Все категории");
        cbCat.Items.AddRange(Db.Products.Select(p => p.Category).Distinct().OrderBy(x => x).ToArray());
        cbCat.SelectedIndex = 0;

        txtSearch.TextChanged += (_, _) => ApplyFilter();
        cbCat.SelectedIndexChanged += (_, _) => ApplyFilter();
        gCatalog.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) AddToCart(); };
        gCatalog.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewRow r in gCatalog.Rows)
                if (r.DataBoundItem is Product p && p.Stock == 0)
                    r.DefaultCellStyle.ForeColor = Color.Gray;
        };
    }

    void ApplyFilter()
    {
        var q = txtSearch.Text.Trim().ToLower();
        var cat = cbCat.SelectedIndex > 0 ? cbCat.SelectedItem as string : null;
        gCatalog.DataSource = Db.Products
            .Where(p => (q == "" || p.Name.ToLower().Contains(q)) && (cat == null || p.Category == cat))
            .OrderBy(p => p.Name)
            .ToList();
    }

    void AddToCart()
    {
        if (gCatalog.CurrentRow?.DataBoundItem is not Product p) return;
        int qty = (int)numQty.Value;
        var item = cart.FirstOrDefault(c => c.ProductId == p.Id);
        int already = item?.Qty ?? 0;

        if (p.Stock == 0)
        {
            MessageBox.Show("Этого товара сейчас нет в наличии.", "Нет в наличии", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (already + qty > p.Stock)
        {
            MessageBox.Show($"Недостаточно товара. Доступно: {p.Stock} шт. (в корзине уже {already}).",
                "Ограничение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (item == null)
            cart.Add(new CartItem { ProductId = p.Id, Name = p.Name, Price = p.Price, Qty = qty });
        else
            item.Qty += qty;

        RefreshCart();
    }

    // ---------- корзина ----------
    void BuildCart()
    {
        Theme.Col(gCart, "Name", "Товар", 3);
        Theme.Col(gCart, "Price", "Цена, ₽", 1, "N2");
        Theme.Col(gCart, "Qty", "Кол-во", 1);
        Theme.Col(gCart, "Sum", "Сумма, ₽", 1, "N2");
        gCart.DataSource = cart;
        tabCart.Controls.Add(gCart);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(4, 6, 0, 0) };
        var bPlus = Theme.Btn("+ 1 шт.", null, 90);
        var bMinus = Theme.Btn("− 1 шт.", null, 90);
        var bDel = Theme.Btn("Удалить", Theme.Danger, 100);
        var bClear = Theme.Btn("Очистить корзину", Theme.Danger, 160);
        bPlus.Click += (_, _) => ChangeQty(+1);
        bMinus.Click += (_, _) => ChangeQty(-1);
        bDel.Click += (_, _) => { if (gCart.CurrentRow?.DataBoundItem is CartItem c) { cart.Remove(c); RefreshCart(); } };
        bClear.Click += (_, _) => { cart.Clear(); RefreshCart(); };
        toolbar.Controls.AddRange(new Control[] { bPlus, bMinus, bDel, bClear });
        tabCart.Controls.Add(toolbar);

        cbBranch.Items.AddRange(Db.Branches);
        cbBranch.SelectedIndex = 0;
        cbPay.Items.AddRange(new object[] { "Картой онлайн", "Наличными при получении" });
        cbPay.SelectedIndex = 0;

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 100, Padding = new Padding(4, 8, 0, 0) };
        var btnBuy = Theme.Btn("Оформить заказ", null, 180);
        btnBuy.Click += (_, _) => Checkout();
        bottom.Controls.AddRange(new Control[]
        {
            Theme.Lbl("Забрать в:"), cbBranch, Theme.Lbl("Оплата:"), cbPay, lblTotal, btnBuy
        });
        tabCart.Controls.Add(bottom);
    }

    void ChangeQty(int delta)
    {
        if (gCart.CurrentRow?.DataBoundItem is not CartItem item) return;
        var p = Db.Products.FirstOrDefault(x => x.Id == item.ProductId);
        if (delta > 0 && (p == null || item.Qty + delta > p.Stock))
        {
            MessageBox.Show("Больше товара на складе нет.", "Ограничение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        item.Qty += delta;
        if (item.Qty <= 0) cart.Remove(item);
        RefreshCart();
    }

    void RefreshCart()
    {
        cart.ResetBindings();
        lblTotal.Text = $"Итого: {cart.Sum(c => c.Sum):N2} ₽";
        tabCart.Text = $"Корзина ({cart.Sum(c => c.Qty)})";
    }

    void Checkout()
    {
        if (cart.Count == 0)
        {
            MessageBox.Show("Корзина пуста.", "Заказ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        foreach (var c in cart)
        {
            var p = Db.Products.FirstOrDefault(x => x.Id == c.ProductId);
            if (p == null || p.Stock < c.Qty)
            {
                MessageBox.Show($"Товара «{c.Name}» уже недостаточно на складе. Измените корзину.", "Заказ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        decimal total = cart.Sum(c => c.Sum);
        if (MessageBox.Show($"Оформить заказ на сумму {total:N2} ₽?\nСпособ оплаты: {cbPay.Text}", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        foreach (var c in cart)
            Db.Products.First(x => x.Id == c.ProductId).Stock -= c.Qty;

        var order = new Order
        {
            Id = Db.NextOrderId(),
            UserLogin = user.Login,
            Date = DateTime.Now,
            Branch = cbBranch.Text,
            Payment = cbPay.Text,
            Status = "Новый",
            Lines = cart.Select(c => new OrderLine { ProductId = c.ProductId, Name = c.Name, Price = c.Price, Qty = c.Qty }).ToList()
        };
        Db.Orders.Add(order);
        Db.Save();

        cart.Clear();
        RefreshCart();
        ApplyFilter();
        LoadOrders();
        MessageBox.Show($"Заказ №{order.Id} оформлен на сумму {total:N2} ₽.\nСпасибо за покупку!", "Готово",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        tabs.SelectedTab = tabOrders;
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
        Theme.Col(gOrders, "Branch", "Аптека", 2.5f);
        Theme.Col(gOrders, "Payment", "Оплата", 1.5f);
        Theme.Col(gOrders, "Status", "Статус", 1);
        Theme.Col(gOrders, "Total", "Сумма, ₽", 1, "N2");
        gOrders.Dock = DockStyle.Top;
        gOrders.Height = 240;
        gOrders.SelectionChanged += (_, _) =>
            gLines.DataSource = (gOrders.CurrentRow?.DataBoundItem as Order)?.Lines;
        tabOrders.Controls.Add(gOrders);

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(4, 6, 0, 0) };
        var bCancel = Theme.Btn("Отменить заказ", Theme.Danger, 170);
        bCancel.Click += (_, _) =>
        {
            if (gOrders.CurrentRow?.DataBoundItem is not Order o) return;
            if (o.Status != "Новый")
            {
                MessageBox.Show("Отменить можно только заказ в статусе «Новый».", "Отмена", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show($"Отменить заказ №{o.Id}?", "Отмена", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Db.Cancel(o);
            LoadOrders();
            ApplyFilter();
        };
        bottom.Controls.Add(bCancel);
        tabOrders.Controls.Add(bottom);
    }

    void LoadOrders()
    {
        gOrders.DataSource = Db.Orders
            .Where(o => o.UserLogin == user.Login)
            .OrderByDescending(o => o.Date)
            .ToList();
    }
}
