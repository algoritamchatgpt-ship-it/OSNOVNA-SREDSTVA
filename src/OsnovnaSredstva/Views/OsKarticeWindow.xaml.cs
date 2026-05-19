using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OsnovnaSredstva.Views;

public partial class OsKarticeWindow : Window
{
    public OsKarticeWindow(OsKarticeViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void OnDodajClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not OsKarticeViewModel vm) return;

        vm.DodajCommand.Execute(null);

        if (GridKartice.SelectedItem == null) return;

        GridKartice.ScrollIntoView(GridKartice.SelectedItem);
        GridKartice.Dispatcher.BeginInvoke(() =>
        {
            GridKartice.CurrentCell = new DataGridCellInfo(GridKartice.SelectedItem, GridKartice.Columns[0]);
            GridKartice.BeginEdit();
        });
    }

    private void OnGridDvoklik(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is OsKarticeViewModel vm)
            vm.KarticaCommand.Execute(null);
    }
}
