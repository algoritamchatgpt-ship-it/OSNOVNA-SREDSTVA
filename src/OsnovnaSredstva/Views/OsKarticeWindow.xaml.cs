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

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is OsKarticeViewModel vm && vm.ImaNeSnimljenih)
        {
            var odg = System.Windows.MessageBox.Show(
                "Imate nesačuvane promjene. Zatvoriti bez čuvanja?",
                "Nesačuvane promjene",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            if (odg != System.Windows.MessageBoxResult.Yes)
                e.Cancel = true;
        }
        base.OnClosing(e);
    }

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
