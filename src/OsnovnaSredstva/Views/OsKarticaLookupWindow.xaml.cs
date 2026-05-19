using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace OsnovnaSredstva.Views;

public partial class OsKarticaLookupWindow : Window
{
    public OsKarticaLookupWindow(OsKarticaLookupViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not OsKarticaLookupViewModel vm) return;
        if (vm.IzabranaStavka == null) return;
        vm.IzaberiCommand.Execute(this);
    }
}
