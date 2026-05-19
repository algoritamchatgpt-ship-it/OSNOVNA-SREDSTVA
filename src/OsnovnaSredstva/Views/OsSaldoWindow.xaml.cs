using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OsnovnaSredstva.Views;

public partial class OsSaldoWindow : Window
{
    public OsSaldoWindow(OsSaldoViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        PrimeniPrikaz(vm);
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void PrimeniPrikaz(OsSaldoViewModel vm)
    {
        if (GridSaldo.Columns.Count == 0) return;

        GridSaldo.Columns[0].Header = vm.NazivKljucneKolone;

        foreach (var c in GridSaldo.Columns)
            c.Visibility = Visibility.Visible;

        switch (vm.Prikaz)
        {
            case OsSaldoViewModel.OsSaldoPrikazTip.Sintetika:
                SakrijKolone(8, 9, 10, 11, 12);
                break;

            case OsSaldoViewModel.OsSaldoPrikazTip.NabavkePoAgrupama:
                SakrijKolone(5, 6, 7, 8, 9, 10, 11, 12);
                break;
        }
    }

    private void SakrijKolone(params int[] indeksi)
    {
        foreach (var idx in indeksi)
        {
            if (idx >= 0 && idx < GridSaldo.Columns.Count)
                GridSaldo.Columns[idx].Visibility = Visibility.Collapsed;
        }
    }
}
