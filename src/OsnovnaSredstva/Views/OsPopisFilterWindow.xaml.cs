using System.Globalization;
using System.Windows;

namespace OsnovnaSredstva.Views;

public enum OsPopisFilterMode
{
    Mrs,
    Poreska
}

public enum OsPopisFilterAction
{
    None,
    PregledSve,
    PregledSkraceni,
    Pregled
}

public sealed class OsPopisFilterData
{
    public string Mesto { get; set; } = "";
    public int Mtr { get; set; }
    public string Konto { get; set; } = "";
    public string Ag { get; set; } = "";
    public string AgPod { get; set; } = "";
    public string Grupa { get; set; } = "";
}

public partial class OsPopisFilterWindow : Window
{
    private readonly OsPopisFilterMode _mode;

    public OsPopisFilterAction Action { get; private set; } = OsPopisFilterAction.None;

    public OsPopisFilterWindow(OsPopisFilterMode mode, OsPopisFilterData? data = null)
    {
        InitializeComponent();
        _mode = mode;

        if (mode == OsPopisFilterMode.Poreska)
        {
            BtnPregledSve.Content = "PREGLED";
            BtnPregledSkr.Visibility = Visibility.Collapsed;
            Title = "PREGLED OSNOVNIH SREDSTAVA - PORESKA";
            TxtInfo.Text = "Kriterijumi poreskog pregleda (prazno polje = svi).";
        }
        else
        {
            Title = "PREGLED OSNOVNIH SREDSTAVA";
            TxtInfo.Text = "Kriterijumi MRS pregleda (prazno polje = svi).";
        }

        if (data != null)
        {
            TxtMesto.Text = data.Mesto ?? string.Empty;
            TxtMtr.Text = data.Mtr <= 0 ? string.Empty : data.Mtr.ToString(CultureInfo.InvariantCulture);
            TxtKonto.Text = data.Konto ?? string.Empty;
            TxtAg.Text = data.Ag ?? string.Empty;
            TxtAgPod.Text = data.AgPod ?? string.Empty;
            TxtGrupa.Text = data.Grupa ?? string.Empty;
        }
    }

    public OsPopisFilterData ReadData()
        => new()
        {
            Mesto = TxtMesto.Text?.Trim() ?? string.Empty,
            Mtr = int.TryParse(TxtMtr.Text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0,
            Konto = TxtKonto.Text?.Trim() ?? string.Empty,
            Ag = TxtAg.Text?.Trim() ?? string.Empty,
            AgPod = TxtAgPod.Text?.Trim() ?? string.Empty,
            Grupa = TxtGrupa.Text?.Trim() ?? string.Empty
        };

    private void OnPregledSveClick(object sender, RoutedEventArgs e)
    {
        Action = _mode == OsPopisFilterMode.Mrs
            ? OsPopisFilterAction.PregledSve
            : OsPopisFilterAction.Pregled;
        DialogResult = true;
    }

    private void OnPregledSkraceniClick(object sender, RoutedEventArgs e)
    {
        Action = OsPopisFilterAction.PregledSkraceni;
        DialogResult = true;
    }

    private void OnIzlazClick(object sender, RoutedEventArgs e)
    {
        Action = OsPopisFilterAction.None;
        Close();
    }
}
