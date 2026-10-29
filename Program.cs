namespace PharmacyPro;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Db.Load();

        // Цикл: вход -> рабочее окно -> (если нажали "Выйти") снова вход
        while (true)
        {
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK) break;

            using ShellForm main = login.Result.Role == Role.Admin
                ? new AdminForm(login.Result)
                : new ClientForm(login.Result);
            main.ShowDialog();

            if (!main.LoggedOut) break; // закрыли крестиком - выходим из программы
        }
    }
}
