using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class OsSifarnikPopisnaListaViewModel : ObservableObject
{
    public enum Tip { VrsteOs, AmortGrupe, AmortPodgrupe, IzvoriFinansiranja, OsnKoriscenja }

    [ObservableProperty] private string _naslov = "";
    [ObservableProperty] private string _poruka = "";

    // Samo jedan od ovih listova je aktivan — ostali su prazni
    public ObservableCollection<OsVrstaStavka>  VrsteOs           { get; } = [];
    public ObservableCollection<OsAgStavka>     AmortGrupe        { get; } = [];
    public ObservableCollection<OsAgPodStavka>  AmortPodgrupe     { get; } = [];
    public ObservableCollection<OsIzvorStavka>  IzvoriFinansiranja{ get; } = [];
    public ObservableCollection<OsOsnKStavka>   OsnKoriscenja     { get; } = [];

    public Tip AktivniTip { get; }

    public bool JeVrste   => AktivniTip == Tip.VrsteOs;
    public bool JeGrupe   => AktivniTip == Tip.AmortGrupe;
    public bool JePodgrupe=> AktivniTip == Tip.AmortPodgrupe;
    public bool JeIzvori  => AktivniTip == Tip.IzvoriFinansiranja;
    public bool JeOsnovi  => AktivniTip == Tip.OsnKoriscenja;

    public OsSifarnikPopisnaListaViewModel(
        Tip tip,
        IEnumerable<OsVrstaStavka>  vrste,
        IEnumerable<OsAgStavka>     grupe,
        IEnumerable<OsAgPodStavka>  podgrupe,
        IEnumerable<OsIzvorStavka>  izvori,
        IEnumerable<OsOsnKStavka>   osnovi)
    {
        AktivniTip = tip;

        Naslov = tip switch
        {
            Tip.VrsteOs            => "POPISNA LISTA — VRSTE OSNOVNIH SREDSTAVA",
            Tip.AmortGrupe         => "POPISNA LISTA — AMORTIZACIONE GRUPE",
            Tip.AmortPodgrupe      => "POPISNA LISTA — PODGRUPE AMORTIZACIJE",
            Tip.IzvoriFinansiranja => "POPISNA LISTA — IZVORI FINANSIRANJA",
            Tip.OsnKoriscenja      => "POPISNA LISTA — OSNOVI KORIŠĆENJA",
            _                      => "POPISNA LISTA"
        };

        foreach (var x in vrste)    VrsteOs.Add(x);
        foreach (var x in grupe)    AmortGrupe.Add(x);
        foreach (var x in podgrupe) AmortPodgrupe.Add(x);
        foreach (var x in izvori)   IzvoriFinansiranja.Add(x);
        foreach (var x in osnovi)   OsnKoriscenja.Add(x);

        var broj = tip switch
        {
            Tip.VrsteOs            => VrsteOs.Count,
            Tip.AmortGrupe         => AmortGrupe.Count,
            Tip.AmortPodgrupe      => AmortPodgrupe.Count,
            Tip.IzvoriFinansiranja => IzvoriFinansiranja.Count,
            Tip.OsnKoriscenja      => OsnKoriscenja.Count,
            _                      => 0
        };
        Poruka = $"Ukupno zapisa: {broj}";
    }

    [RelayCommand]
    private void Stampa(System.Windows.Controls.DataGrid? grid)
    {
        if (grid == null) { MessageBox.Show("Tabela nije dostupna.", Naslov, MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var dlg = new System.Windows.Controls.PrintDialog();
        if (dlg.ShowDialog() != true) return;
        dlg.PrintVisual(grid, Naslov);
        Poruka = "Štampa poslata na štampač.";
    }
}
