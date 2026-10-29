using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PharmacyPro;

public class DbData
{
    public List<Product> Products { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
}

// Простое хранилище: все данные лежат в одном JSON-файле (AppData\PharmacyPro\data.json)
public static class Db
{
    public static readonly string[] Branches =
    {
        "Аптека №1 — ул. Ленина, 12",
        "Аптека №2 — пр. Мира, 45",
        "Аптека №3 — ул. Советская, 7",
    };
    public static readonly string[] Statuses = { "Новый", "Собран", "Выдан", "Отменён" };

    static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PharmacyPro");
    static readonly string FilePath = Path.Combine(Dir, "data.json");
    static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    static DbData data = new();

    public static List<Product> Products => data.Products;
    public static List<User> Users => data.Users;
    public static List<Order> Orders => data.Orders;

    public static string Hash(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
                data = JsonSerializer.Deserialize<DbData>(File.ReadAllText(FilePath), Options) ?? new DbData();
        }
        catch { data = new DbData(); }

        bool changed = false;
        if (data.Users.Count == 0) { SeedUsers(); changed = true; }
        if (data.Products.Count == 0) { SeedProducts(); changed = true; }
        if (changed) Save();
    }

    public static void Save()
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(data, Options));
    }

    public static int NextProductId() => Products.Count == 0 ? 1 : Products.Max(p => p.Id) + 1;
    public static int NextOrderId() => Orders.Count == 0 ? 1 : Orders.Max(o => o.Id) + 1;

    // Отмена заказа: возвращаем товары на склад
    public static void Cancel(Order o)
    {
        if (o.Status == "Отменён") return;
        foreach (var l in o.Lines)
        {
            var p = Products.FirstOrDefault(x => x.Id == l.ProductId);
            if (p != null) p.Stock += l.Qty;
        }
        o.Status = "Отменён";
        Save();
    }

    static void SeedUsers()
    {
        Users.Add(new User { Login = "admin", PasswordHash = Hash("admin123"), FullName = "Администратор", Role = Role.Admin });
        Users.Add(new User { Login = "client", PasswordHash = Hash("client123"), FullName = "Иван Петров", Role = Role.Client });
    }

    static void SeedProducts()
    {
        void Add(string n, string c, decimal p, int s) =>
            Products.Add(new Product { Id = NextProductId(), Name = n, Category = c, Price = p, Stock = s });

        Add("Парацетамол 500 мг", "Жаропонижающие", 45.50m, 100);
        Add("Ибупрофен 200 мг", "Обезболивающие", 120m, 60);
        Add("Активированный уголь", "Желудок и кишечник", 35m, 150);
        Add("Витамин C 1000 мг", "Витамины", 310m, 40);
        Add("Нурофен сироп", "Детские", 285m, 25);
        Add("Пластырь бактерицидный", "Перевязочные", 28m, 200);
        Add("Аскорбиновая кислота", "Витамины", 20m, 120);
        Add("Но-шпа 40 мг", "Спазмолитики", 150m, 80);
        Add("Бинт стерильный", "Перевязочные", 40m, 90);
        Add("Градусник электронный", "Приборы", 450m, 15);
    }
}
