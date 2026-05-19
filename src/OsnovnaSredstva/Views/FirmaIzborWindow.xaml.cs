using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class FirmaIzborWindow : Window
{
    public event Action? FirmaIzabrana;
    public event Action? OtkacenoPrijavljeni;

    private readonly FirmaIzborViewModel _vm;

    public FirmaIzborWindow(FirmaIzborViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        vm.FirmaIzabrana += () => FirmaIzabrana?.Invoke();
        vm.OtkacenoPrijavljeni += () => OtkacenoPrijavljeni?.Invoke();

        LstFirme.MouseDoubleClick += (_, _) =>
        {
            if (_vm.PotvrdiCommand.CanExecute(null))
                _vm.PotvrdiCommand.Execute(null);
        };

        Loaded += async (_, _) => await vm.UcitajAsync();
    }
}
