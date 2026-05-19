using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OsnovnaSredstva.Views;

public partial class OsEvidencijaWindow : Window
{
    public OsEvidencijaWindow(OsEvidencijaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void OnDodajClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not OsEvidencijaViewModel vm) return;
        vm.DodajCommand.Execute(null);
        if (GridKartice.SelectedItem == null) return;
        GridKartice.ScrollIntoView(GridKartice.SelectedItem);
        GridKartice.Dispatcher.BeginInvoke(() =>
        {
            if (GridKartice.Columns.Count > 0)
            {
                GridKartice.CurrentCell = new DataGridCellInfo(GridKartice.SelectedItem, GridKartice.Columns[0]);
                GridKartice.BeginEdit();
            }
        });
    }

    private void OnGridDvoklik(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is OsEvidencijaViewModel vm)
            vm.PregledKarticaCommand.Execute(null);
    }
}
