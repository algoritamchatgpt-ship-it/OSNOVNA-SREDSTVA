using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OsnovnaSredstva.Views;

public partial class OsSifarnikWindow : Window
{
    public OsSifarnikWindow(OsSifarnikViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is OsSifarnikViewModel vm && vm.ImaNeSnimljenih)
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
        if (DataContext is not OsSifarnikViewModel vm) return;

        vm.DodajCommand.Execute(null);

        var grid = vm.AktivniTab switch
        {
            0 => (DataGrid?)GridVrste,
            1 => GridGrupe,
            2 => GridPodgrupe,
            3 => GridIzvori,
            4 => GridOsnovi,
            _ => null
        };

        if (grid?.SelectedItem == null) return;

        grid.ScrollIntoView(grid.SelectedItem);
        grid.Dispatcher.BeginInvoke(() =>
        {
            grid.CurrentCell = new DataGridCellInfo(grid.SelectedItem, grid.Columns[0]);
            grid.BeginEdit();
        });
    }

    private void OnGridDvoklik(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is OsSifarnikViewModel vm)
            vm.KarticaCommand.Execute(null);
    }
}
