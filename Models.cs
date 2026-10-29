namespace PharmacyPro;

public enum Role { Client, Admin }

public class User
{
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public Role Role { get; set; }
    public string RoleText => Role == Role.Admin ? "Администратор" : "Клиент";
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class CartItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Qty { get; set; }
    public decimal Sum => Price * Qty;
}

public class OrderLine
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Qty { get; set; }
    public decimal Sum => Price * Qty;
}

public class Order
{
    public int Id { get; set; }
    public string UserLogin { get; set; } = "";
    public DateTime Date { get; set; }
    public string Branch { get; set; } = "";
    public string Payment { get; set; } = "";
    public string Status { get; set; } = "Новый";
    public List<OrderLine> Lines { get; set; } = new();
    public decimal Total => Lines.Sum(l => l.Sum);
}
