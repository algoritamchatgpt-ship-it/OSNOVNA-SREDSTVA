using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OsnovnaSredstva.Views;

public partial class OsObrazacOaWindow : Window
{
    public OsObrazacOaWindow(OsObrazacOaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void OnDodajClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not OsObrazacOaViewModel vm) return;
        vm.DodajCommand.Execute(null);
        if (GridStavke.SelectedItem == null) return;
        GridStavke.ScrollIntoView(GridStavke.SelectedItem);
        GridStavke.Dispatcher.BeginInvoke(() =>
        {
            if (GridStavke.Columns.Count > 0)
            {
                GridStavke.CurrentCell = new DataGridCellInfo(GridStavke.SelectedItem, GridStavke.Columns[0]);
                GridStavke.BeginEdit();
            }
        });
    }

    private void OnUcitajGrupeClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not OsObrazacOaViewModel vm) return;
        if (vm.UcitajGrupeCommand.CanExecute(null))
            vm.UcitajGrupeCommand.Execute(null);
    }

    private void OnUcitajPodatkeClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not OsObrazacOaViewModel vm) return;
        if (vm.UcitajPodatkeCommand.CanExecute(null))
            vm.UcitajPodatkeCommand.Execute(null);
    }

    private void OnGridDvoklik(object sender, MouseButtonEventArgs e) { }
}
