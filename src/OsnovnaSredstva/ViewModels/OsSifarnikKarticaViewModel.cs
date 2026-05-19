using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class OsSifarnikKarticaViewModel : ObservableObject
{
    private readonly int _tabIndex;

    // Originalni objekti — upisujemo nazad kad Potvrdi
    private OsVrstaStavka?  _origVrsta;
    private OsAgStavka?     _origAg;
    private OsAgPodStavka?  _origAgPod;
    private OsIzvorStavka?  _origIzvor;
    private OsOsnKStavka?   _origOsnov;

    [ObservableProperty] private string _naslov = "";

    // ─── Tab 0: Vrsta OS ───
    [ObservableProperty] private string _vrstaKod   = "";
    [ObservableProperty] private string _vrstaNaziv = "";

    // ─── Tab 1: Amortizaciona grupa ───
    [ObservableProperty] private string  _agKod   = "";
    [ObservableProperty] private decimal _agStopa;
    [ObservableProperty] private string  _agOpis  = "";
    [ObservableProperty] private string  _agVrsta = "";

    // ─── Tab 2: Podgrupa amortizacije ───
    [ObservableProperty] private string _agPodKod   = "";
    [ObservableProperty] private string _agPodAgKod = "";
    [ObservableProperty] private string _agPodOpis  = "";

    // ─── Tab 3: Izvor finansiranja ───
    [ObservableProperty] private string _izvorKod   = "";
    [ObservableProperty] private string _izvorNaziv = "";

    // ─── Tab 4: Osnov korišćenja ───
    [ObservableProperty] private string _osnovKorKod   = "";
    [ObservableProperty] private string _osnovKorNaziv = "";

    // Vidljivost sekcija — setuju se jednom pri konstrukciji
    public bool JeVrsta  => _tabIndex == 0;
    public bool JeAg     => _tabIndex == 1;
    public bool JeAgPod  => _tabIndex == 2;
    public bool JeIzvor  => _tabIndex == 3;
    public bool JeOsnov  => _tabIndex == 4;

    private OsSifarnikKarticaViewModel(int tabIndex) => _tabIndex = tabIndex;

    // ─── Fabričke metode ───

    public static OsSifarnikKarticaViewModel ZaVrstu(OsVrstaStavka s)
    {
        var vm = new OsSifarnikKarticaViewModel(0) { _origVrsta = s };
        vm.Naslov    = "VRSTA OSNOVNOG SREDSTVA";
        vm.VrstaKod  = s.Vrsta;
        vm.VrstaNaziv = s.Naziv;
        return vm;
    }

    public static OsSifarnikKarticaViewModel ZaAg(OsAgStavka s)
    {
        var vm = new OsSifarnikKarticaViewModel(1) { _origAg = s };
        vm.Naslov  = "AMORTIZACIONA GRUPA";
        vm.AgKod   = s.Ag;
        vm.AgStopa = s.AgStopa;
        vm.AgOpis  = s.Opis;
        vm.AgVrsta = s.Vrsta;
        return vm;
    }

    public static OsSifarnikKarticaViewModel ZaAgPod(OsAgPodStavka s)
    {
        var vm = new OsSifarnikKarticaViewModel(2) { _origAgPod = s };
        vm.Naslov      = "PODGRUPA AMORTIZACIJE";
        vm.AgPodKod    = s.AgPod;
        vm.AgPodAgKod  = s.Ag;
        vm.AgPodOpis   = s.Opis;
        return vm;
    }

    public static OsSifarnikKarticaViewModel ZaIzvor(OsIzvorStavka s)
    {
        var vm = new OsSifarnikKarticaViewModel(3) { _origIzvor = s };
        vm.Naslov      = "IZVOR FINANSIRANJA";
        vm.IzvorKod    = s.Izvor;
        vm.IzvorNaziv  = s.Naziv;
        return vm;
    }

    public static OsSifarnikKarticaViewModel ZaOsnov(OsOsnKStavka s)
    {
        var vm = new OsSifarnikKarticaViewModel(4) { _origOsnov = s };
        vm.Naslov        = "OSNOV KORIŠĆENJA";
        vm.OsnovKorKod   = s.OsnovKor;
        vm.OsnovKorNaziv = s.Naziv;
        return vm;
    }

    // ─── Komande ───

    [RelayCommand]
    private void Potvrdi(Window? window)
    {
        if (!Validuj()) return;
        PrimiPromene();
        if (window != null) window.DialogResult = true;
    }

    [RelayCommand]
    private void Otkazi(Window? window)
    {
        if (window != null) window.DialogResult = false;
    }

    // ─── Validacija ───

    private bool Validuj()
    {
        var (kodPolje, kodVrednost, opisPolje, opisVrednost) = _tabIndex switch
        {
            0 => ("Šifra vrste",   VrstaKod,    "Naziv vrste",    VrstaNaziv),
            1 => ("Šifra grupe",   AgKod,       "Opis grupe",     AgOpis),
            2 => ("Šifra podgrupe",AgPodKod,    "Opis podgrupe",  AgPodOpis),
            3 => ("Šifra izvora",  IzvorKod,    "Naziv izvora",   IzvorNaziv),
            4 => ("Šifra osnova",  OsnovKorKod, "Naziv osnova",   OsnovKorNaziv),
            _ => ("Šifra", "", "Naziv", "")
        };

        if (string.IsNullOrWhiteSpace(kodVrednost))
        {
            MessageBox.Show($"{kodPolje} je obavezna.", Naslov, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (string.IsNullOrWhiteSpace(opisVrednost))
        {
            MessageBox.Show($"{opisPolje} je obavezan.", Naslov, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    // ─── Primjena promjena na originalne objekte ───

    private void PrimiPromene()
    {
        if (_origVrsta != null)
        {
            _origVrsta.Vrsta = VrstaKod.Trim();
            _origVrsta.Naziv = VrstaNaziv.Trim();
        }
        if (_origAg != null)
        {
            _origAg.Ag      = AgKod.Trim();
            _origAg.AgStopa = AgStopa;
            _origAg.Opis    = AgOpis.Trim();
            _origAg.Vrsta   = AgVrsta.Trim();
        }
        if (_origAgPod != null)
        {
            _origAgPod.AgPod = AgPodKod.Trim();
            _origAgPod.Ag    = AgPodAgKod.Trim();
            _origAgPod.Opis  = AgPodOpis.Trim();
        }
        if (_origIzvor != null)
        {
            _origIzvor.Izvor = IzvorKod.Trim();
            _origIzvor.Naziv = IzvorNaziv.Trim();
        }
        if (_origOsnov != null)
        {
            _origOsnov.OsnovKor = OsnovKorKod.Trim();
            _origOsnov.Naziv    = OsnovKorNaziv.Trim();
        }
    }
}
