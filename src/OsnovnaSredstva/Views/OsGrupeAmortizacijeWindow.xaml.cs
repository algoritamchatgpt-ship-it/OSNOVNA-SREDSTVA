using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OsnovnaSredstva.Views;

public partial class OsGrupeAmortizacijeWindow : Window
{
    public OsGrupeAmortizacijeWindow(OsGrupeAmortizacijeViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void OnDodajClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not OsGrupeAmortizacijeViewModel vm) return;
        vm.DodajCommand.Execute(null);

        if (GridGrupe.SelectedItem == null) return;
        GridGrupe.ScrollIntoView(GridGrupe.SelectedItem);
        GridGrupe.Dispatcher.BeginInvoke(() =>
        {
            GridGrupe.CurrentCell = new DataGridCellInfo(GridGrupe.SelectedItem, GridGrupe.Columns[0]);
            GridGrupe.BeginEdit();
        });
    }
}
