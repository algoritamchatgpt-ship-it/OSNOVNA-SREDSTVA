using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OsnovnaSredstva.Views;

public partial class OsSifarnikPopisnaListaWindow : Window
{
    public OsSifarnikPopisnaListaWindow(OsSifarnikPopisnaListaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void OnStampajClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not OsSifarnikPopisnaListaViewModel vm) return;

        DataGrid? grid = vm.AktivniTip switch
        {
            OsSifarnikPopisnaListaViewModel.Tip.VrsteOs            => GridVrste,
            OsSifarnikPopisnaListaViewModel.Tip.AmortGrupe         => GridGrupe,
            OsSifarnikPopisnaListaViewModel.Tip.AmortPodgrupe      => GridPodgrupe,
            OsSifarnikPopisnaListaViewModel.Tip.IzvoriFinansiranja => GridIzvori,
            OsSifarnikPopisnaListaViewModel.Tip.OsnKoriscenja      => GridOsnovi,
            _ => null
        };

        if (grid == null) return;

        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;
        dlg.PrintVisual(grid, vm.Naslov);
        vm.Poruka = "Štampa poslata na štampač.";
    }
}
