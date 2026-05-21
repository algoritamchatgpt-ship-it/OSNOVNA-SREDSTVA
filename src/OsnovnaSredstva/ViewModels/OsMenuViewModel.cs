using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Views;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class OsMenuViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly IPutanjaService _putanjaService;

    public event Action? OdjavaSeTrazena;
    public event Action? VratiseFirmaIzboru;

    public OsMenuViewModel(AppState appState, IPutanjaService putanjaService)
    {
        _appState = appState;
        _putanjaService = putanjaService;
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

    [RelayCommand]
    private void OtvoriKartice()
    {
        var vm = new OsKarticeViewModel(_appState);
        var win = new OsKarticeWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriEvidenciju()
    {
        var vm = new OsEvidencijaViewModel(_appState, "os.dbf");
        var win = new OsEvidencijaWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriArhivu()
    {
        var vm = new OsArhivaViewModel(_appState);
        var win = new OsArhivaWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriStampa()
    {
        var vm = new OsObrazacOaViewModel(_appState);
        var win = new OsObrazacOaWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriSifarNici()
    {
        var vm = new OsSifarnikViewModel(_appState);
        var win = new OsSifarnikWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriLozinke()
    {
        var vm = new FormiranjeLozinkiViewModel(_putanjaService);
        var win = new FormiranjeLozinkiWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriMesta()
    {
        var vm = new GradoviViewModel(_appState, _putanjaService);
        var win = new GradoviWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriPartnere()
    {
        if (_appState.AktivnaFirma is null)
        {
            MessageBox.Show("Nema aktivne firme.", "Partneri", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var vm = new PartneriViewModel(_appState);
        var win = new PartneriWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriIzvoz()
    {
        var vm = new IzvozTabelaViewModel(_appState, _putanjaService);
        var win = new IzvozTabelaWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriPrenosu()
    {
        if (_appState.AktivnaFirma is null)
        {
            MessageBox.Show("Nema aktivne firme.", "Prenos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var vm = new OsPrenosaViewModel(_appState);
        var win = new OsPrenosaWindow(vm);
        win.ShowDialog();
        OnPropertyChanged(nameof(Godina));
        OnPropertyChanged(nameof(NaslovHeader));
    }

    [RelayCommand]
    private void OtvoriPreglede()
    {
        if (_appState.AktivnaFirma is null)
        {
            MessageBox.Show("Nema aktivne firme.", "Pregledi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var vm = new OsPreglediViewModel(_appState);
        var win = new OsPreglediWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriGrupeAmortizacije()
    {
        var vm = new OsGrupeAmortizacijeViewModel(_appState);
        var win = new OsGrupeAmortizacijeWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriPodatkeOs()
    {
        if (_appState.AktivnaFirma is null)
        {
            MessageBox.Show("Nema aktivne firme.", "Podaci OS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var vm = new OsPodaciViewModel(_appState);
        var win = new OsPodaciWindow(vm);
        win.ShowDialog();
    }
}
