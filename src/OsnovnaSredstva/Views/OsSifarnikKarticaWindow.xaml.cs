using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OsnovnaSredstva.Views;

public partial class OsSifarnikKarticaWindow : Window
{
    public OsSifarnikKarticaWindow(OsSifarnikKarticaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnContentRendered(object sender, EventArgs e)
    {
        if (DataContext is not OsSifarnikKarticaViewModel vm) return;

        TextBox? polje = vm switch
        {
            { JeVrsta  : true } => PrvoPoljeVrsta,
            { JeAg     : true } => PrvoPoljeAg,
            { JeAgPod  : true } => PrvoPoljeAgPod,
            { JeIzvor  : true } => PrvoPoljeIzvor,
            { JeOsnov  : true } => PrvoPoljeOsnov,
            _ => null
        };

        polje?.Focus();
        polje?.SelectAll();
    }
}
