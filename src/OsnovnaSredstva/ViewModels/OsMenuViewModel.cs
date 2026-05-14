using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Views;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class OsMenuViewModel : ObservableObject
{
    private readonly AppState _appState;

    public event Action? OdjavaSeTrazena;
    public event Action? VratiseFirmaIzboru;

    public OsMenuViewModel(AppState appState)
    {
        _appState = appState;
    }

    public string NazivFirme => _appState.AktivnaFirma?.Naziv ?? "—";
    public string FolderFirme => _appState.AktivnaFirma?.FolderIme ?? "—";
    public string KorisnikIme => _appState.TrenutniKorisnik?.KorisnikIme ?? "—";
    public string Godina => _appState.AktivnaGodina.ToString();

    public string NaslovHeader =>
        $"{NazivFirme}   |   {FolderFirme}   |   {KorisnikIme}   |   {Godina}";

    [RelayCommand]
    private void OtvoriPodatkeOFirmi()
    {
        var vm = new FirmaPodaciViewModel(_appState);
        var win = new FirmaPodaciWindow(vm);
        win.ShowDialog();
        OnPropertyChanged(nameof(NazivFirme));
        OnPropertyChanged(nameof(NaslovHeader));
    }

    [RelayCommand]
    private void Odjava()
    {
        _appState.Odjavi();
        OdjavaSeTrazena?.Invoke();
    }

    [RelayCommand]
    private void PromenaFirme()
    {
        _appState.PostaviFirmu(null!);
        VratiseFirmaIzboru?.Invoke();
    }

    [RelayCommand]
    private void PromenaGodine()
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            "Unesite poslovnu godinu:", "Promena godine",
            _appState.AktivnaGodina.ToString());

        if (int.TryParse(input, out var godina) && godina >= 2000 && godina <= 2100)
        {
            _appState.AktivnaGodina = godina;
            OnPropertyChanged(nameof(Godina));
            OnPropertyChanged(nameof(NaslovHeader));
        }
    }

    // ═══ PLACEHOLDER KOMANDE ZA MENIJE ═══
    // Ove komande prikazuju poruku dok se modul ne implementira

    [RelayCommand]
    private void OtvoriKartice()
    {
        MessageBox.Show("Modul Kartice osnovnih sredstava — u razvoju.",
            "OS — Kartice", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void OtvoriAmortizacija()
    {
        MessageBox.Show("Modul Amortizacija — u razvoju.",
            "OS — Amortizacija", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void OtvoriGrupe()
    {
        MessageBox.Show("Modul Grupe osnovnih sredstava — u razvoju.",
            "OS — Grupe", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void OtvoriRashodovanje()
    {
        MessageBox.Show("Modul Rashodovanje / Otuđenje — u razvoju.",
            "OS — Rashodovanje", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void OtvoriPrenos()
    {
        MessageBox.Show("Modul Prenos podataka u novu godinu — u razvoju.",
            "OS — Prenos", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void OtvoriStampa()
    {
        MessageBox.Show("Modul Štampa kartica i izveštaja — u razvoju.",
            "OS — Štampa", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void OtvoriInventar()
    {
        MessageBox.Show("Modul Inventar osnovnih sredstava — u razvoju.",
            "OS — Inventar", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void OtvoriKnjizenje()
    {
        MessageBox.Show("Modul Knjiženje u glavnu knjigu — u razvoju.",
            "OS — Knjiženje", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void OtvoriSifarNici()
    {
        var vm = new OsSifarnikViewModel(_appState);
        var win = new OsSifarnikWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriParametri()
    {
        MessageBox.Show("Modul Parametri sistema — u razvoju.",
            "OS — Parametri", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
