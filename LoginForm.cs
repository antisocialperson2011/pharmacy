namespace PharmacyPro;

public class LoginForm : Form
{
    public User Result { get; private set; }

    readonly TextBox txtLogin = new();
    readonly TextBox txtPass = new() { UseSystemPasswordChar = true };
    readonly TextBox txtName = new();
    readonly Label lblTitle = new();
    readonly Button btnMain = Theme.Btn("Войти", null, 300);
    readonly LinkLabel lnkMode = new() { Text = "Нет аккаунта? Зарегистрироваться", AutoSize = true, LinkColor = Theme.Primary };
    readonly Panel nameBox = new() { Left = 40, Top = 262, Width = 300, Height = 64, Visible = false };
    bool register;

    public LoginForm()
    {
        Text = "Вход — Аптека «Здоровье»";
        Font = Theme.Base;
        BackColor = Theme.Bg;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(380, 470);

        var header = new Panel { Dock = DockStyle.Top, Height = 110, BackColor = Theme.Primary };
        header.Controls.Add(new Label
        {
            Text = "✚  Аптека «Здоровье»",
            ForeColor = Color.White,
            Font = Theme.Title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        });
        Controls.Add(header);

        Theme.Place(this, "Логин", txtLogin, 40, 135, 300);
        Theme.Place(this, "Пароль", txtPass, 40, 198, 300);

        Theme.Place(nameBox, "Ваше имя", txtName, 0, 0, 300);
        Controls.Add(nameBox);

        btnMain.Left = 40;
        btnMain.Top = 290;
        btnMain.Click += (_, _) => Submit();
        Controls.Add(btnMain);
        AcceptButton = btnMain;

        lnkMode.Left = 40;
        lnkMode.Top = 345;
        lnkMode.Click += (_, _) => ToggleMode();
        Controls.Add(lnkMode);

        Controls.Add(new Label
        {
            Text = "Демо: admin / admin123, client / client123",
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = true,
            Left = 40,
            Top = 435
        });
    }

    void ToggleMode()
    {
        register = !register;
        nameBox.Visible = register;
        btnMain.Text = register ? "Создать аккаунт" : "Войти";
        btnMain.Top = register ? 345 : 290;
        lnkMode.Text = register ? "Уже есть аккаунт? Войти" : "Нет аккаунта? Зарегистрироваться";
        lnkMode.Top = register ? 400 : 345;
    }

    void Submit()
    {
        var login = txtLogin.Text.Trim();
        var pass = txtPass.Text;

        if (login == "" || pass == "")
        {
            MessageBox.Show("Введите логин и пароль.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!register)
        {
            var u = Db.Users.FirstOrDefault(x =>
                x.Login.Equals(login, StringComparison.OrdinalIgnoreCase) && x.PasswordHash == Db.Hash(pass));
            if (u == null)
            {
                MessageBox.Show("Неверный логин или пароль.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Result = u;
            DialogResult = DialogResult.OK;
        }
        else
        {
            if (txtName.Text.Trim() == "")
            {
                MessageBox.Show("Введите ваше имя.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (pass.Length < 4)
            {
                MessageBox.Show("Пароль должен быть не короче 4 символов.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (Db.Users.Any(x => x.Login.Equals(login, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Такой логин уже занят.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var u = new User { Login = login, PasswordHash = Db.Hash(pass), FullName = txtName.Text.Trim(), Role = Role.Client };
            Db.Users.Add(u);
            Db.Save();
            Result = u;
            DialogResult = DialogResult.OK;
        }
    }
}
