using System.Windows;

namespace OsnovnaSredstva.Views;

public enum OsSaldoKontaIzborAction
{
    None,
    SaldoSintetika,
    SaldoAnalitika,
    SaldoNabavkePoAg,
    PocetnoStanje
}

public partial class OsSaldoKontaIzborWindow : Window
{
    public OsSaldoKontaIzborAction Action { get; private set; } = OsSaldoKontaIzborAction.None;

    public OsSaldoKontaIzborWindow()
    {
        InitializeComponent();
    }

    private void OnSaldoSintetikaClick(object sender, RoutedEventArgs e)
    {
        Action = OsSaldoKontaIzborAction.SaldoSintetika;
        DialogResult = true;
    }

    private void OnSaldoAnalitikaClick(object sender, RoutedEventArgs e)
    {
        Action = OsSaldoKontaIzborAction.SaldoAnalitika;
        DialogResult = true;
    }

    private void OnSaldoNabavkePoAgClick(object sender, RoutedEventArgs e)
    {
        Action = OsSaldoKontaIzborAction.SaldoNabavkePoAg;
        DialogResult = true;
    }

    private void OnPocetnoStanjeClick(object sender, RoutedEventArgs e)
    {
        Action = OsSaldoKontaIzborAction.PocetnoStanje;
        DialogResult = true;
    }

    private void OnIzlazClick(object sender, RoutedEventArgs e)
    {
        Action = OsSaldoKontaIzborAction.None;
        Close();
    }
}
