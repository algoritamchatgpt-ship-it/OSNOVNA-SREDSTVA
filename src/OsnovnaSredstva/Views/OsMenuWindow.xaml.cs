using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class OsMenuWindow : Window
{
    private readonly OsMenuViewModel _vm;

    public OsMenuWindow(OsMenuViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        vm.OdjavaSeTrazena += () => { /* handled in App */ };
        vm.VratiseFirmaIzboru += () => { /* handled in App */ };
    }
}
