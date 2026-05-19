using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class LoginWindow : Window
{
    public event Action? PrijavaUspela;
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        vm.PrijavaUspela += () => PrijavaUspela?.Invoke();

        BtnIzlaz.Click += (_, _) => Application.Current.Shutdown();

        Loaded += (_, _) => TxtKorisnik.Focus();
    }

    private void TxtLozinka_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _vm.Lozinka = TxtLozinka.Password;
    }
}
