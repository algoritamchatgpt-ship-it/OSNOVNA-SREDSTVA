using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;
using System.Globalization;
using System.IO;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class OsKarticaKarticaViewModel : ObservableObject
{
    private sealed record LookupDef(string[] DbfKandidati, string SifraPolje, string NazivPolje, string? DodatnoPolje = null);
    private sealed record LookupInfo(string Naziv, string Dodatno);

    private readonly OsKartica _original;
    private readonly AppState _appState;
    private readonly Dictionary<OsKarticaLookupTip, Dictionary<string, LookupInfo>> _lookupCache = [];

    [ObservableProperty] private string _naslov;
    [ObservableProperty] private string _podnaslov;

    [ObservableProperty] private string _sifraOs = string.Empty;
    [ObservableProperty] private string _nazivOs = string.Empty;

    [ObservableProperty] private string _datumNabavke = string.Empty;
    [ObservableProperty] private string _datumPocetkaAmortizacije = string.Empty;
    [ObservableProperty] private string _datumProdaje = string.Empty;
    [ObservableProperty] private string _godinaProizvodnje = string.Empty;
    [ObservableProperty] private string _vrsta = string.Empty;
    [ObservableProperty] private string _povrsina = string.Empty;
    [ObservableProperty] private string _strukturaObjekta = string.Empty;
    [ObservableProperty] private string _brojParcele = string.Empty;
    [ObservableProperty] private string _katastarskaOpstina = string.Empty;
    [ObservableProperty] private string _ispravaSvojine1 = string.Empty;
    [ObservableProperty] private string _ispravaSvojine2 = string.Empty;
    [ObservableProperty] private string _ispravaSvojine3 = string.Empty;
    [ObservableProperty] private string _osnovKoriscenja = string.Empty;
    [ObservableProperty] private string _izvorFinansiranjaNabavke = string.Empty;
    [ObservableProperty] private string _nacinSticanja = string.Empty;
    [ObservableProperty] private string _brojDatumOdlukeRashod = string.Empty;
    [ObservableProperty] private string _mestoLokacije = string.Empty;
    [ObservableProperty] private string _mestoTroskova = string.Empty;
    [ObservableProperty] private string _konto = string.Empty;
    [ObservableProperty] private string _nomenklaturniBroj = string.Empty;
    [ObservableProperty] private string _inventarskiBroj = string.Empty;
    [ObservableProperty] private string _grupa = string.Empty;
    [ObservableProperty] private string _amortizacionaGrupa = string.Empty;
    [ObservableProperty] private string _amortizacionaPodgrupa = string.Empty;

    [ObservableProperty] private string _vrstaNaziv = string.Empty;
    [ObservableProperty] private string _osnovKoriscenjaNaziv = string.Empty;
    [ObservableProperty] private string _izvorFinansiranjaNaziv = string.Empty;
    [ObservableProperty] private string _mestoLokacijeNaziv = string.Empty;
    [ObservableProperty] private string _mestoTroskovaNaziv = string.Empty;
    [ObservableProperty] private string _kontoNaziv = string.Empty;
    [ObservableProperty] private string _amortizacionaGrupaNaziv = string.Empty;
    [ObservableProperty] private string _amortizacionaPodgrupaNaziv = string.Empty;

    [ObservableProperty] private string _firma = string.Empty;
    [ObservableProperty] private string _nacinObracuna = string.Empty;
    [ObservableProperty] private string _kolicina = string.Empty;
    [ObservableProperty] private string _stopaOtpisaMrs = string.Empty;
    [ObservableProperty] private string _stopaOtpisaPorProp = string.Empty;
    [ObservableProperty] private string _pocetnaNabavnaMrs = string.Empty;
    [ObservableProperty] private string _pocetnaIspravkaMrs = string.Empty;
    [ObservableProperty] private string _pocetnaSadasnjaMrs = string.Empty;
    [ObservableProperty] private string _pocetnaNabavnaPp = string.Empty;
    [ObservableProperty] private string _pocetnaIspravkaPp = string.Empty;
    [ObservableProperty] private string _pocetnaSadasnjaPp = string.Empty;
    [ObservableProperty] private string _procenaKoriscenja = string.Empty;
    [ObservableProperty] private string _iznosNaknadnogUlaganja = string.Empty;
    [ObservableProperty] private string _datumNaknadnogUlaganja = string.Empty;

    public OsKarticaKarticaViewModel(OsKartica original, AppState appState)
    {
        _original = original;
        _appState = appState;

        Naslov = "KARTICA OSNOVNOG SREDSTVA";
        Podnaslov = "Polja rasporedjena za unos kao na fox kartici.";

        SifraOs = original.Osifra;
        NazivOs = original.Naz;

        DatumNabavke = FormatDate(original.DatNab);
        DatumPocetkaAmortizacije = FormatDate(GetExtraDate("DATSTARTAM"));
        DatumProdaje = FormatDate(GetExtraDate("DATPROD"));
        GodinaProizvodnje = FormatInt(GetExtraInt("GODPRO"));
        Vrsta = original.Vrsta;
        Povrsina = FormatDecimal(GetExtraDecimal("POVRSINA"), 3);
        StrukturaObjekta = GetExtraString("STRUKT1");
        BrojParcele = GetExtraString("PARCELA");
        KatastarskaOpstina = GetExtraString("KATASTO");
        IspravaSvojine1 = GetExtraString("ISPRAVE1");
        IspravaSvojine2 = GetExtraString("ISPRAVE2");
        IspravaSvojine3 = GetExtraString("ISPRAVE3");
        OsnovKoriscenja = original.OsnovKor;
        IzvorFinansiranjaNabavke = original.Izvor;
        NacinSticanja = GetExtraString("NACINSTIC");
        BrojDatumOdlukeRashod = GetExtraString("RASHOD");
        MestoLokacije = GetExtraString("MESTOL");
        MestoTroskova = original.Mesto;
        Konto = original.Konto;
        NomenklaturniBroj = GetExtraString("NOMENKL");
        InventarskiBroj = original.InvBroj;
        Grupa = GetExtraString("GRUPA");
        AmortizacionaGrupa = original.Ag;
        AmortizacionaPodgrupa = original.AgPod;

        Firma = GetExtraString("NAZIV");
        NacinObracuna = GetExtraString("NACINOB");
        Kolicina = FormatDecimal(original.Kom, 2);
        StopaOtpisaMrs = FormatDecimal(original.StopaOt, 3);
        StopaOtpisaPorProp = FormatDecimal(GetExtraDecimal("STOPAOT2"), 3);
        PocetnaNabavnaMrs = FormatDecimal(original.Nab0, 2);
        PocetnaIspravkaMrs = FormatDecimal(original.Isp0, 2);
        PocetnaSadasnjaMrs = FormatDecimal(original.Sad0, 2);
        PocetnaNabavnaPp = FormatDecimal(GetExtraDecimal("NAB02"), 2);
        PocetnaIspravkaPp = FormatDecimal(GetExtraDecimal("ISP02"), 2);
        PocetnaSadasnjaPp = FormatDecimal(GetExtraDecimal("SAD02"), 2);
        ProcenaKoriscenja = FormatDecimal(GetExtraDecimal("PROCENA"), 2);
        IznosNaknadnogUlaganja = FormatDecimal(GetExtraDecimal("IZNOSULAG"), 2);
        DatumNaknadnogUlaganja = FormatDate(GetExtraDate("DATULAG"));

        OsveziLookupNazive();
    }

    partial void OnVrstaChanged(string value)
        => VrstaNaziv = ResolveLookupNaziv(OsKarticaLookupTip.Vrsta, value);

    partial void OnOsnovKoriscenjaChanged(string value)
        => OsnovKoriscenjaNaziv = ResolveLookupNaziv(OsKarticaLookupTip.OsnovKoriscenja, value);

    partial void OnIzvorFinansiranjaNabavkeChanged(string value)
        => IzvorFinansiranjaNaziv = ResolveLookupNaziv(OsKarticaLookupTip.IzvorFinansiranja, value);

    partial void OnMestoLokacijeChanged(string value)
        => MestoLokacijeNaziv = ResolveLookupNaziv(OsKarticaLookupTip.Mesto, value);

    partial void OnMestoTroskovaChanged(string value)
        => MestoTroskovaNaziv = ResolveLookupNaziv(OsKarticaLookupTip.Mesto, value);

    partial void OnKontoChanged(string value)
        => KontoNaziv = ResolveLookupNaziv(OsKarticaLookupTip.Konto, value);

    partial void OnAmortizacionaGrupaChanged(string value)
        => AmortizacionaGrupaNaziv = ResolveLookupNaziv(OsKarticaLookupTip.AmortizacionaGrupa, value);

    partial void OnAmortizacionaPodgrupaChanged(string value)
    {
        AmortizacionaPodgrupaNaziv = ResolveLookupNaziv(OsKarticaLookupTip.AmortizacionaPodgrupa, value);

        var info = ResolveLookupInfo(OsKarticaLookupTip.AmortizacionaPodgrupa, value);
        var agIzPodgrupe = (info?.Dodatno ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(agIzPodgrupe) &&
            !string.Equals((AmortizacionaGrupa ?? string.Empty).Trim(), agIzPodgrupe, StringComparison.OrdinalIgnoreCase))
        {
            AmortizacionaGrupa = agIzPodgrupe;
        }
    }

    // SAD0 = NAB0 - ISP0  (MRS — automatski kao u Fox VALID eventu)
    partial void OnPocetnaNabavnaMrsChanged(string value) => IzracunajSad0Mrs();
    partial void OnPocetnaIspravkaMrsChanged(string value) => IzracunajSad0Mrs();

    private void IzracunajSad0Mrs()
    {
        if (TryParseDecimalQuiet(PocetnaNabavnaMrs,  out var nabVal) &&
            TryParseDecimalQuiet(PocetnaIspravkaMrs, out var ispVal))
        {
            var sad = (nabVal ?? 0m) - (ispVal ?? 0m);
            var formatted = sad.ToString("F2", System.Globalization.CultureInfo.CurrentCulture);
            if (PocetnaSadasnjaMrs != formatted)
                PocetnaSadasnjaMrs = formatted;
        }
    }

    // SAD02 = NAB02 - ISP02  (PP — automatski kao u Fox VALID eventu)
    partial void OnPocetnaNabavnaPpChanged(string value) => IzracunajSad0Pp();
    partial void OnPocetnaIspravkaPpChanged(string value) => IzracunajSad0Pp();

    private void IzracunajSad0Pp()
    {
        if (TryParseDecimalQuiet(PocetnaNabavnaPp,  out var nabVal) &&
            TryParseDecimalQuiet(PocetnaIspravkaPp, out var ispVal))
        {
            var sad = (nabVal ?? 0m) - (ispVal ?? 0m);
            var formatted = sad.ToString("F2", System.Globalization.CultureInfo.CurrentCulture);
            if (PocetnaSadasnjaPp != formatted)
                PocetnaSadasnjaPp = formatted;
        }
    }

    private static bool TryParseDecimalQuiet(string input, out decimal? result)
    {
        result = null;
        var raw = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw)) { result = 0m; return true; }
        if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var d) ||
            decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d))
        {
            result = d;
            return true;
        }
        return false;
    }

    [RelayCommand]
    private void LookupVrsta(Window? owner)
    {
        OtvoriLookup(owner, OsKarticaLookupTip.Vrsta, Vrsta, sifra => Vrsta = sifra);
    }

    [RelayCommand]
    private void LookupOsnovKoriscenja(Window? owner)
    {
        OtvoriLookup(owner, OsKarticaLookupTip.OsnovKoriscenja, OsnovKoriscenja, sifra => OsnovKoriscenja = sifra);
    }

    [RelayCommand]
    private void LookupIzvorFinansiranja(Window? owner)
    {
        OtvoriLookup(owner, OsKarticaLookupTip.IzvorFinansiranja, IzvorFinansiranjaNabavke, sifra => IzvorFinansiranjaNabavke = sifra);
    }

    [RelayCommand]
    private void LookupMesto(Window? owner)
    {
        OtvoriLookup(owner, OsKarticaLookupTip.Mesto, MestoTroskova, sifra => MestoTroskova = sifra);
    }

    [RelayCommand]
    private void LookupMestoLokacije(Window? owner)
    {
        OtvoriLookup(owner, OsKarticaLookupTip.Mesto, MestoLokacije, sifra => MestoLokacije = sifra);
    }

    [RelayCommand]
    private void LookupKonto(Window? owner)
    {
        OtvoriLookup(owner, OsKarticaLookupTip.Konto, Konto, sifra => Konto = sifra);
    }

    [RelayCommand]
    private void LookupAmortizacionaGrupa(Window? owner)
    {
        OtvoriLookup(owner, OsKarticaLookupTip.AmortizacionaGrupa, AmortizacionaGrupa, sifra => AmortizacionaGrupa = sifra);
    }

    [RelayCommand]
    private void LookupAmortizacionaPodgrupa(Window? owner)
    {
        OtvoriLookup(owner, OsKarticaLookupTip.AmortizacionaPodgrupa, AmortizacionaPodgrupa,
            sifra => AmortizacionaPodgrupa = sifra,
            stavka =>
            {
                var ag = (stavka.Dodatno ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(ag))
                    AmortizacionaGrupa = ag;
            });
    }

    [RelayCommand]
    private void Potvrdi(Window? window)
    {
        if (string.IsNullOrWhiteSpace(SifraOs))
        {
            MessageBox.Show("Sifra OS je obavezna.", Naslov, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(NazivOs))
        {
            MessageBox.Show("Naziv osnovnog sredstva je obavezan.", Naslov, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseDate(DatumNabavke, "Datum nabavke", out var datNab)) return;
        if (!TryParseDate(DatumPocetkaAmortizacije, "Datum pocetka amortizacije", out var datStartAm)) return;
        if (!TryParseDate(DatumProdaje, "Datum prodaje", out var datProd)) return;
        if (!TryParseDate(DatumNaknadnogUlaganja, "Datum naknadnog ulaganja", out var datUlag)) return;

        if (!TryParseInt(GodinaProizvodnje, "Godina", out var godina)) return;

        if (!TryParseDecimal(Povrsina, "Povrsina", out var povrsina)) return;
        if (!TryParseDecimal(Kolicina, "Kolicina", out var kolicina)) return;
        if (!TryParseDecimal(StopaOtpisaMrs, "Stopa otpisa MRS", out var stopaMrs)) return;
        if (!TryParseDecimal(StopaOtpisaPorProp, "Stopa otpisa por.prop.", out var stopaPor)) return;
        if (!TryParseDecimal(PocetnaNabavnaMrs, "Pocetna nabavna MRS", out var nabMrs)) return;
        if (!TryParseDecimal(PocetnaIspravkaMrs, "Pocetna ispravka MRS", out var ispMrs)) return;
        if (!TryParseDecimal(PocetnaSadasnjaMrs, "Pocetna sadasnja MRS", out var sadMrs)) return;
        if (!TryParseDecimal(PocetnaNabavnaPp, "Pocetna nabavna PP", out var nabPp)) return;
        if (!TryParseDecimal(PocetnaIspravkaPp, "Pocetna ispravka PP", out var ispPp)) return;
        if (!TryParseDecimal(PocetnaSadasnjaPp, "Pocetna sadasnja PP", out var sadPp)) return;
        if (!TryParseDecimal(ProcenaKoriscenja, "Procena koriscenja", out var procena)) return;
        if (!TryParseDecimal(IznosNaknadnogUlaganja, "Iznos naknadnog ulaganja", out var iznosUlag)) return;

        _original.Osifra = SifraOs.Trim();
        _original.Naz = NazivOs.Trim();
        _original.DatNab = datNab;
        _original.Vrsta = Vrsta.Trim();
        _original.OsnovKor = OsnovKoriscenja.Trim();
        _original.Izvor = IzvorFinansiranjaNabavke.Trim();
        _original.Mesto = MestoTroskova.Trim();
        _original.Konto = Konto.Trim();
        _original.InvBroj = InventarskiBroj.Trim();
        _original.Ag = AmortizacionaGrupa.Trim();
        _original.AgPod = AmortizacionaPodgrupa.Trim();
        _original.Kom = kolicina ?? 0m;
        _original.StopaOt = stopaMrs ?? 0m;
        _original.Nab0 = nabMrs ?? 0m;
        _original.Isp0 = ispMrs ?? 0m;
        _original.Sad0 = sadMrs ?? 0m;

        SetExtra("DATSTARTAM", datStartAm);
        SetExtra("DATPROD", datProd);
        SetExtra("GODPRO", godina);
        SetExtra("POVRSINA", povrsina);
        SetExtra("STRUKT1", StrukturaObjekta.Trim());
        SetExtra("PARCELA", BrojParcele.Trim());
        SetExtra("KATASTO", KatastarskaOpstina.Trim());
        SetExtra("ISPRAVE1", IspravaSvojine1.Trim());
        SetExtra("ISPRAVE2", IspravaSvojine2.Trim());
        SetExtra("ISPRAVE3", IspravaSvojine3.Trim());
        SetExtra("NACINSTIC", NacinSticanja.Trim());
        SetExtra("RASHOD", BrojDatumOdlukeRashod.Trim());
        SetExtra("MESTOL", MestoLokacije.Trim());
        SetExtra("NOMENKL", NomenklaturniBroj.Trim());
        SetExtra("GRUPA", Grupa.Trim());

        SetExtra("NAZIV", Firma.Trim());
        SetExtra("NACINOB", NacinObracuna.Trim());
        SetExtra("STOPAOT2", stopaPor);
        SetExtra("NAB02", nabPp);
        SetExtra("ISP02", ispPp);
        SetExtra("SAD02", sadPp);
        SetExtra("PROCENA", procena);
        SetExtra("IZNOSULAG", iznosUlag);
        SetExtra("DATULAG", datUlag);

        if (window != null)
            window.DialogResult = true;
    }

    [RelayCommand]
    private void Otkazi(Window? window)
    {
        if (window != null)
            window.DialogResult = false;
    }

    private void OtvoriLookup(
        Window? owner,
        OsKarticaLookupTip tip,
        string inicijalnaSifra,
        Action<string> postaviSifru,
        Action<OsKarticaLookupStavka>? posleIzbora = null)
    {
        try
        {
            var vm = new OsKarticaLookupViewModel(_appState, tip, inicijalnaSifra);
            var prozor = new OsKarticaLookupWindow(vm);
            if (owner != null)
                prozor.Owner = owner;

            if (prozor.ShowDialog() == true && vm.IzabranaStavka != null)
            {
                postaviSifru(vm.IzabranaSifra);
                posleIzbora?.Invoke(vm.IzabranaStavka);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lookup forma: {ex.Message}", Naslov, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OsveziLookupNazive()
    {
        VrstaNaziv = ResolveLookupNaziv(OsKarticaLookupTip.Vrsta, Vrsta);
        OsnovKoriscenjaNaziv = ResolveLookupNaziv(OsKarticaLookupTip.OsnovKoriscenja, OsnovKoriscenja);
        IzvorFinansiranjaNaziv = ResolveLookupNaziv(OsKarticaLookupTip.IzvorFinansiranja, IzvorFinansiranjaNabavke);
        MestoLokacijeNaziv = ResolveLookupNaziv(OsKarticaLookupTip.Mesto, MestoLokacije);
        MestoTroskovaNaziv = ResolveLookupNaziv(OsKarticaLookupTip.Mesto, MestoTroskova);
        KontoNaziv = ResolveLookupNaziv(OsKarticaLookupTip.Konto, Konto);
        AmortizacionaGrupaNaziv = ResolveLookupNaziv(OsKarticaLookupTip.AmortizacionaGrupa, AmortizacionaGrupa);
        AmortizacionaPodgrupaNaziv = ResolveLookupNaziv(OsKarticaLookupTip.AmortizacionaPodgrupa, AmortizacionaPodgrupa);
    }

    private string ResolveLookupNaziv(OsKarticaLookupTip tip, string sifra)
    {
        var info = ResolveLookupInfo(tip, sifra);
        if (info == null)
            return string.Empty;

        if (tip == OsKarticaLookupTip.Mesto && !string.IsNullOrWhiteSpace(info.Dodatno))
            return $"{info.Naziv} ({info.Dodatno})";

        if (tip == OsKarticaLookupTip.AmortizacionaPodgrupa && !string.IsNullOrWhiteSpace(info.Dodatno))
            return $"{info.Naziv} [AG {info.Dodatno}]";

        return info.Naziv;
    }

    private LookupInfo? ResolveLookupInfo(OsKarticaLookupTip tip, string sifra)
    {
        var key = (sifra ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var index = GetLookupIndex(tip);
        return index.TryGetValue(key, out var info) ? info : null;
    }

    private Dictionary<string, LookupInfo> GetLookupIndex(OsKarticaLookupTip tip)
    {
        if (_lookupCache.TryGetValue(tip, out var cache))
            return cache;

        var def = GetLookupDef(tip);
        var result = new Dictionary<string, LookupInfo>(StringComparer.OrdinalIgnoreCase);
        var path = PronadjiDbf(def.DbfKandidati);
        if (string.IsNullOrWhiteSpace(path))
        {
            _lookupCache[tip] = result;
            return result;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(path);
            foreach (var z in zapisi)
            {
                var sifra = DbfReader.Str(z, def.SifraPolje).Trim();
                if (string.IsNullOrWhiteSpace(sifra))
                    continue;

                var naziv = DbfReader.Str(z, def.NazivPolje).Trim();
                var dodatno = string.IsNullOrWhiteSpace(def.DodatnoPolje) ? string.Empty : DbfReader.Str(z, def.DodatnoPolje).Trim();
                result[sifra] = new LookupInfo(naziv, dodatno);
            }
        }
        catch
        {
            // Ako lookup tabela nije citljiva, kartica ostaje funkcionalna,
            // samo bez prikaza naziva uz sifru.
        }

        _lookupCache[tip] = result;
        return result;
    }

    private string? PronadjiDbf(IEnumerable<string> kandidati)
    {
        var folderFirme = _appState.AktivnaFirma?.FolderPath;

        var folderKandidati = new List<string>();

        void DodajFolder(string? f)
        {
            if (!string.IsNullOrWhiteSpace(f) &&
                !folderKandidati.Any(x => string.Equals(x, f, StringComparison.OrdinalIgnoreCase)))
                folderKandidati.Add(f);
        }

        if (!string.IsNullOrWhiteSpace(folderFirme))
        {
            DodajFolder(folderFirme);
            DodajFolder(Path.Combine(folderFirme, "data00"));
            var root = FinWorkspaceResolver.NormalizeRootPath(folderFirme);
            DodajFolder(Path.Combine(root, "data00"));
            DodajFolder(root);
        }

        DodajFolder(Path.Combine(AppContext.BaseDirectory, "data00"));
        DodajFolder(AppContext.BaseDirectory);

        var kandidatiLista = kandidati.ToList();
        foreach (var folder in folderKandidati)
        {
            if (!Directory.Exists(folder)) continue;
            foreach (var ime in kandidatiLista)
            {
                foreach (var naziv in new[] { ime, ime.ToUpperInvariant() })
                {
                    var putanja = Path.Combine(folder, naziv);
                    if (File.Exists(putanja)) return putanja;
                }
            }
            var svi = Directory.GetFiles(folder, "*.dbf", SearchOption.TopDirectoryOnly);
            foreach (var ime in kandidatiLista)
            {
                var nadjen = svi.FirstOrDefault(f =>
                    Path.GetFileName(f).Equals(ime, StringComparison.OrdinalIgnoreCase));
                if (nadjen != null) return nadjen;
            }
        }

        return null;
    }

    private static LookupDef GetLookupDef(OsKarticaLookupTip tip) => tip switch
    {
        OsKarticaLookupTip.Vrsta => new LookupDef(["osvrsta.dbf"], "VRSTA", "NAZIV"),
        OsKarticaLookupTip.OsnovKoriscenja => new LookupDef(["ososnk.dbf"], "OSNOVKOR", "NAZIV"),
        OsKarticaLookupTip.IzvorFinansiranja => new LookupDef(["osizvorf.dbf"], "IZVOR", "NAZIV"),
        OsKarticaLookupTip.AmortizacionaGrupa => new LookupDef(["osag.dbf"], "AG", "OPIS", "VRSTA"),
        OsKarticaLookupTip.AmortizacionaPodgrupa => new LookupDef(["osagpod.dbf"], "AGPOD", "OPIS", "AG"),
        OsKarticaLookupTip.Mesto => new LookupDef(["mesta.dbf"], "MP", "MESTO", "POSTA"),
        OsKarticaLookupTip.Konto => new LookupDef(["konto.dbf"], "KONTO", "OPIS"),
        _ => throw new ArgumentOutOfRangeException(nameof(tip), tip, null)
    };

    private string GetExtraString(string key)
    {
        if (_original.ExtraPolja.TryGetValue(key, out var value) && value != null)
            return Convert.ToString(value, CultureInfo.CurrentCulture)?.Trim() ?? string.Empty;

        return string.Empty;
    }

    private decimal? GetExtraDecimal(string key)
    {
        if (!_original.ExtraPolja.TryGetValue(key, out var value) || value == null)
            return null;

        return value switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double d => Convert.ToDecimal(d, CultureInfo.InvariantCulture),
            _ when decimal.TryParse(Convert.ToString(value, CultureInfo.CurrentCulture), NumberStyles.Any, CultureInfo.CurrentCulture, out var d) => d,
            _ when decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
            _ => null,
        };
    }

    private int? GetExtraInt(string key)
    {
        if (!_original.ExtraPolja.TryGetValue(key, out var value) || value == null)
            return null;

        return value switch
        {
            int i => i,
            long l => (int)l,
            decimal d => (int)d,
            _ when int.TryParse(Convert.ToString(value, CultureInfo.CurrentCulture), NumberStyles.Any, CultureInfo.CurrentCulture, out var i) => i,
            _ when int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var i) => i,
            _ => null,
        };
    }

    private DateTime? GetExtraDate(string key)
    {
        if (!_original.ExtraPolja.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is DateTime dt)
            return dt;

        var raw = Convert.ToString(value, CultureInfo.CurrentCulture);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt)
            || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;

        return null;
    }

    private void SetExtra(string key, object? value)
    {
        _original.ExtraPolja[key] = value;
    }

    private bool TryParseDate(string input, string nazivPolja, out DateTime? result)
    {
        result = null;
        var raw = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (DateTime.TryParseExact(raw,
            ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "yyyyMMdd"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dt)
            || DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
        {
            result = dt;
            return true;
        }

        MessageBox.Show($"Polje '{nazivPolja}' mora biti datum (npr. 15.05.2026).", Naslov, MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private bool TryParseDecimal(string input, string nazivPolja, out decimal? result)
    {
        result = null;
        var raw = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out var d)
            || decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
        {
            result = d;
            return true;
        }

        MessageBox.Show($"Polje '{nazivPolja}' mora biti broj.", Naslov, MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private bool TryParseInt(string input, string nazivPolja, out int? result)
    {
        result = null;
        var raw = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (int.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out var i)
            || int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out i))
        {
            result = i;
            return true;
        }

        MessageBox.Show($"Polje '{nazivPolja}' mora biti ceo broj.", Naslov, MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private static string FormatDate(DateTime? value)
        => value?.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture) ?? string.Empty;

    private static string FormatDecimal(decimal? value, int decimals)
        => value.HasValue ? value.Value.ToString($"F{decimals}", CultureInfo.CurrentCulture) : string.Empty;

    private static string FormatDecimal(decimal value, int decimals)
        => value.ToString($"F{decimals}", CultureInfo.CurrentCulture);

    private static string FormatInt(int? value)
        => value?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
}
