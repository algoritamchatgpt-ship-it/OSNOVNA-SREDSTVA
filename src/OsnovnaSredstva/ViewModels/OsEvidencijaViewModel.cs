using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;
using System.Collections.ObjectModel;
using System.IO;

namespace OsnovnaSredstva.ViewModels;

public partial class OsEvidencijaViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly string _dbfIme;

    private List<OsKartica> _sveKartice = [];
    private List<OsKartica> _osnovniPoredak = [];
    private int _sortOrder;

    [ObservableProperty] private ObservableCollection<OsKartica> _kartice = [];
    [ObservableProperty] private OsKartica? _izabranaKartica;
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private string _filterText = "";

    private bool _izmijenjeno;
    public bool ImaNeSnimljenih => _izmijenjeno;

    public string NazivIzabrane =>
        IzabranaKartica == null ? "" : $"{IzabranaKartica.Osifra}   {IzabranaKartica.Naz}";

    public string InfoIzabrane =>
        IzabranaKartica == null ? "" :
        $"Konto: {IzabranaKartica.Konto}   Mesto: {IzabranaKartica.Mesto}   InvBroj: {IzabranaKartica.InvBroj}   AG: {IzabranaKartica.Ag}   AgPod: {IzabranaKartica.AgPod}";

    partial void OnIzabranaKarticaChanged(OsKartica? value)
    {
        OnPropertyChanged(nameof(NazivIzabrane));
        OnPropertyChanged(nameof(InfoIzabrane));
    }

    partial void OnFilterTextChanged(string value) => PrimeniFiIlter();

    private static readonly HashSet<string> PoznataPolja = new(StringComparer.OrdinalIgnoreCase)
    {
        "OSIFRA","NAZ","DATNAB","BRNAL","KONTO","VRSTA",
        "AG","AGPOD","INVBROJ","MESTO","NAB0","ISP0","SAD0",
        "KOM","CENA","STOPAOT","OSNOVKOR","IZVOR","PRENETO","IDBR"
    };

    public OsEvidencijaViewModel(AppState appState, string dbfIme = "os.dbf")
    {
        _appState = appState;
        _dbfIme = dbfIme;
        Ucitaj();
    }

    private void PrimeniFiIlter()
    {
        var f = FilterText?.Trim() ?? "";
        Kartice = string.IsNullOrEmpty(f)
            ? new ObservableCollection<OsKartica>(_sveKartice)
            : new ObservableCollection<OsKartica>(
                _sveKartice.Where(k =>
                    (k.Osifra ?? "").ToLowerInvariant().Contains(f.ToLowerInvariant()) ||
                    (k.Naz    ?? "").ToLowerInvariant().Contains(f.ToLowerInvariant())));
    }

    private void Ucitaj()
    {
        var path = DbfPutanja(_dbfIme);
        if (path == null) { Kartice = []; Poruka = $"{_dbfIme} nije pronađen u folderu firme."; return; }

        try
        {
            var reader = new SimpleDbfReader(path);
            var stavke = new List<OsKartica>();

            foreach (var r in reader.Zapisi())
            {
                var k = new OsKartica
                {
                    Osifra   = r.DajString("OSIFRA"),
                    Naz      = r.DajString("NAZ"),
                    DatNab   = r.DajDate("DATNAB"),
                    BrNal    = r.DajString("BRNAL"),
                    Konto    = r.DajString("KONTO"),
                    Vrsta    = r.DajString("VRSTA"),
                    Ag       = r.DajString("AG"),
                    AgPod    = r.DajString("AGPOD"),
                    InvBroj  = r.DajString("INVBROJ"),
                    Mesto    = r.DajString("MESTO"),
                    Nab0     = r.DajDecimal("NAB0"),
                    Isp0     = r.DajDecimal("ISP0"),
                    Sad0     = r.DajDecimal("SAD0"),
                    Kom      = r.DajDecimal("KOM"),
                    Cena     = r.DajDecimal("CENA"),
                    StopaOt  = r.DajDecimal("STOPAOT"),
                    OsnovKor = r.DajString("OSNOVKOR"),
                    Izvor    = r.DajString("IZVOR"),
                    Preneto  = r.DajString("PRENETO"),
                    IDBr     = (int)r.DajDecimal("IDBR"),
                };

                foreach (var field in reader.Fields)
                {
                    if (!PoznataPolja.Contains(field.Name))
                    {
                        k.ExtraPolja[field.Name] = field.Type switch
                        {
                            'D'        => (object?)r.DajDate(field.Name),
                            'N' or 'F' => r.DajDecimal(field.Name),
                            'L'        => r.DajBool(field.Name),
                            _          => r.DajString(field.Name)
                        };
                    }
                }

                stavke.Add(k);
            }

            _sveKartice = stavke;
            _osnovniPoredak = [.. stavke];
            PrimeniFiIlter();
            Poruka = $"Učitano {_sveKartice.Count} zapisa iz {_dbfIme}.";
            _izmijenjeno = false;
        }
        catch (Exception ex)
        {
            _sveKartice = [];
            _osnovniPoredak = [];
            Kartice = [];
            Poruka = $"Greška: {ex.Message}";
        }
    }

    [RelayCommand] private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Dodaj()
    {
        var max = _sveKartice.Select(k => k.IDBr).DefaultIfEmpty(0).Max();
        var nova = new OsKartica { IDBr = max + 1, Preneto = "N" };
        _sveKartice.Add(nova);
        _osnovniPoredak.Add(nova);
        PrimeniFiIlter();
        IzabranaKartica = nova;
        Poruka = "Novi red dodan. Unesite podatke i kliknite Sačuvaj.";
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        var path = DbfPutanja(_dbfIme);
        if (path == null) { Poruka = $"{_dbfIme} nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, _sveKartice,
                (k, f) => f.ToUpperInvariant() switch
                {
                    "OSIFRA"   => (object?)k.Osifra,
                    "NAZ"      => k.Naz,
                    "DATNAB"   => k.DatNab,
                    "BRNAL"    => k.BrNal,
                    "KONTO"    => k.Konto,
                    "VRSTA"    => k.Vrsta,
                    "AG"       => k.Ag,
                    "AGPOD"    => k.AgPod,
                    "INVBROJ"  => k.InvBroj,
                    "MESTO"    => k.Mesto,
                    "NAB0"     => k.Nab0,
                    "ISP0"     => k.Isp0,
                    "SAD0"     => k.Sad0,
                    "KOM"      => k.Kom,
                    "CENA"     => k.Cena,
                    "STOPAOT"  => k.StopaOt,
                    "OSNOVKOR" => k.OsnovKor,
                    "IZVOR"    => k.Izvor,
                    "PRENETO"  => k.Preneto,
                    "IDBR"     => (object?)k.IDBr,
                    _          => k.ExtraPolja.TryGetValue(f, out var v) ? v : null
                });
            Poruka = $"Sačuvano ({_sveKartice.Count} zapisa).";
            _izmijenjeno = false;
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    [RelayCommand]
    private void Sort()
    {
        _sortOrder = _sortOrder >= 8 ? 0 : _sortOrder + 1;

        _sveKartice = _sortOrder switch
        {
            1 => [.. _sveKartice.OrderBy(k => (k.Osifra ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            2 => [.. _sveKartice.OrderBy(k => (k.Naz ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            3 => [.. _sveKartice.OrderBy(k => (k.Konto ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            4 => [.. _sveKartice.OrderBy(k => (k.Ag ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            5 => [.. _sveKartice.OrderBy(k => (k.AgPod ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            6 => [.. _sveKartice.OrderBy(k => (k.InvBroj ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            7 => [.. _sveKartice.OrderBy(k => (k.Mesto ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            8 => [.. _sveKartice.OrderBy(k => k.IDBr)],
            _ => [.. _osnovniPoredak]
        };

        var staraSifra = IzabranaKartica?.Osifra;
        PrimeniFiIlter();
        if (!string.IsNullOrWhiteSpace(staraSifra))
        {
            var vrati = Kartice.FirstOrDefault(k => string.Equals(k.Osifra, staraSifra, StringComparison.OrdinalIgnoreCase));
            if (vrati != null) IzabranaKartica = vrati;
        }

        Poruka = $"Sortirano (redosled {_sortOrder}/8).";
    }

    [RelayCommand] private void Prvi()   { if (Kartice.Count > 0) IzabranaKartica = Kartice[0]; }
    [RelayCommand] private void Zadnji() { if (Kartice.Count > 0) IzabranaKartica = Kartice[^1]; }
    [RelayCommand] private void Dole()
    {
        if (IzabranaKartica == null || Kartice.Count == 0) return;
        var idx = Kartice.IndexOf(IzabranaKartica);
        if (idx < Kartice.Count - 1) IzabranaKartica = Kartice[idx + 1];
    }
    [RelayCommand] private void Gore()
    {
        if (IzabranaKartica == null || Kartice.Count == 0) return;
        var idx = Kartice.IndexOf(IzabranaKartica);
        if (idx > 0) IzabranaKartica = Kartice[idx - 1];
    }

    [RelayCommand]
    private void TrazenjeSifre()
    {
        var dlg = new OsEvidencijaPretragaWindow(
            "TRAZENJE SIFRE",
            "SIFRA OSNOVNOG SREDSTVA",
            4);

        if (dlg.ShowDialog() != true) return;

        var unos = dlg.Unos.Trim();
        if (string.IsNullOrWhiteSpace(unos)) return;

        int.TryParse(unos, out var trazeniBroj);
        var nadjeno = _sveKartice
            .OrderBy(k => (k.Osifra ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(k =>
            {
                var sifra = (k.Osifra ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(sifra)) return false;
                if (int.TryParse(sifra, out var broj) && trazeniBroj > 0) return broj == trazeniBroj;
                return sifra.StartsWith(unos, StringComparison.OrdinalIgnoreCase);
            });

        if (nadjeno != null)
        {
            SelektujKarticu(nadjeno);
            Poruka = $"Pronađena: {nadjeno.Osifra?.Trim()} - {nadjeno.Naz}";
            return;
        }

        Poruka = $"Sifra '{unos}' nije pronađena.";
    }

    [RelayCommand]
    private void TrazenjeInventarnogBroja()
    {
        var dlg = new OsEvidencijaPretragaWindow(
            "TRAZENJE INVENTARNOG BROJA",
            "INVENTARNI BROJ OSNOVNOG SREDSTVA",
            20);

        if (dlg.ShowDialog() != true) return;

        var unos = dlg.Unos.Trim();
        if (string.IsNullOrWhiteSpace(unos)) return;

        var nadjeno = _sveKartice
            .OrderBy(k => (k.InvBroj ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(k =>
                (k.InvBroj ?? string.Empty).Trim().StartsWith(unos, StringComparison.OrdinalIgnoreCase));

        if (nadjeno != null)
        {
            SelektujKarticu(nadjeno);
            Poruka = $"Pronađena: {nadjeno.Osifra?.Trim()} - {nadjeno.Naz} (InvBroj: {nadjeno.InvBroj?.Trim()})";
            return;
        }

        Poruka = $"Inventarski broj '{unos}' nije pronađen.";
    }

    [RelayCommand] private void PregledKartica()
    {
        if (IzabranaKartica == null) { Poruka = "Nije izabran red."; return; }
        var vm = new OsKarticaKarticaViewModel(IzabranaKartica, _appState);
        var win = new OsKarticaKarticaWindow(vm);
        if (win.ShowDialog() == true) { Poruka = "Kartica ažurirana. Kliknite Sačuvaj."; _izmijenjeno = true; }
    }

    [RelayCommand]
    private void ZadnjeUPocetno()
    {
        if (System.Windows.MessageBox.Show(
                $"Prenos tekućih u početne vrijednosti za SVE kartice ({_sveKartice.Count})?\n\n" +
                "Ovo ažurira NAB0, ISP0, SAD0 i resetuje period (NAB, ISP, AMORT = 0).",
                "Zadnje u početno",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        foreach (var k in _sveKartice)
        {
            var noviNab0 = k.Nab0 + XDec(k, "NAB");
            var noviIsp0 = k.Isp0 + XDec(k, "ISP");
            k.Nab0 = noviNab0;
            k.Isp0 = noviIsp0;
            k.Sad0 = noviNab0 - noviIsp0;
            k.ExtraPolja["NAB"]   = 0m;
            k.ExtraPolja["ISP"]   = 0m;
            k.ExtraPolja["SAD"]   = noviNab0 - noviIsp0;
            k.ExtraPolja["AMORT"] = 0m;

            var isp2 = XDec(k, "ISP2");
            var sad2 = XDec(k, "SAD2");
            k.ExtraPolja["ISP02"]  = isp2;
            k.ExtraPolja["SAD02"]  = sad2;
            k.ExtraPolja["NAB02"]  = 0m;
            k.ExtraPolja["NAB2"]   = 0m;
            k.ExtraPolja["ISP2"]   = isp2;
            k.ExtraPolja["SAD2"]   = sad2;
            k.ExtraPolja["AMORT2"] = 0m;
        }
        Poruka = $"Zadnje preneseno u početne za {_sveKartice.Count} kartica. Kliknite Sačuvaj.";
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void SaldoKonta()
    {
        var izbor = new OsSaldoKontaIzborWindow();
        if (izbor.ShowDialog() != true) return;

        var (periodOd, _) = ProcitajPeriod();

        var vm = izbor.Action switch
        {
            OsSaldoKontaIzborAction.SaldoSintetika => OsSaldoViewModel.PoKontuSintetika(_sveKartice),
            OsSaldoKontaIzborAction.SaldoAnalitika => OsSaldoViewModel.PoKontu(_sveKartice),
            OsSaldoKontaIzborAction.SaldoNabavkePoAg => OsSaldoViewModel.SaldoNabavkePoAgrupama(_sveKartice, periodOd),
            OsSaldoKontaIzborAction.PocetnoStanje => OsSaldoViewModel.PocetnoStanjePoAg(_sveKartice, periodOd),
            _ => null
        };

        if (vm == null) return;
        new OsSaldoWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void SaldoMesta()
    {
        var dlg = new OsEvidencijaPretragaWindow(
            "SALDO PO MESTIMA",
            "KONTO",
            8);

        var konto = string.Empty;
        if (dlg.ShowDialog() == true)
            konto = dlg.Unos?.Trim() ?? string.Empty;

        var vm = OsSaldoViewModel.PoMestu(_sveKartice, konto);
        new OsSaldoWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PregledMrs()
    {
        var kriterijumi = UcitajPopisKriterijume();
        var wnd = new OsPopisFilterWindow(OsPopisFilterMode.Mrs, kriterijumi);
        if (wnd.ShowDialog() != true) return;

        kriterijumi = wnd.ReadData();
        SacuvajPopisKriterijume(kriterijumi);

        var filtrirano = PrimeniPopisFilter(_sveKartice, kriterijumi);
        var skraceni = wnd.Action == OsPopisFilterAction.PregledSkraceni;
        var vm = OsMrsViewModel.MrsPregled(filtrirano, skraceni);
        new OsMrsWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PreglPoreskaStara()
    {
        var kriterijumi = UcitajPopisKriterijume();
        var wnd = new OsPopisFilterWindow(OsPopisFilterMode.Poreska, kriterijumi);
        if (wnd.ShowDialog() != true) return;

        kriterijumi = wnd.ReadData();
        SacuvajPopisKriterijume(kriterijumi);

        var filtrirano = PrimeniPopisFilter(_sveKartice, kriterijumi)
            .Where(k => string.IsNullOrWhiteSpace(DajExtra(k, "NACINOB")));

        var vm = OsMrsViewModel.PoreskaStara(filtrirano);
        new OsMrsWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PreglPoreskaNova()
    {
        var kriterijumi = UcitajPopisKriterijume();
        var wnd = new OsPopisFilterWindow(OsPopisFilterMode.Poreska, kriterijumi);
        if (wnd.ShowDialog() != true) return;

        kriterijumi = wnd.ReadData();
        SacuvajPopisKriterijume(kriterijumi);

        var filtrirano = PrimeniPopisFilter(_sveKartice, kriterijumi)
            .Where(k => !string.IsNullOrWhiteSpace(DajExtra(k, "NACINOB")));

        var vm = OsMrsViewModel.PoreskaNova(filtrirano);
        new OsMrsWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void Prenos()
    {
        var vm = new OsPrenosaViewModel(_appState);
        new OsPrenosaWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void Podaci()
    {
        var vm = new OsPodaciViewModel(_appState);
        var win = new OsPodaciWindow(vm);
        win.ShowDialog();
        Ucitaj();
        Poruka = vm.Poruka;
    }

    [RelayCommand]
    private void Obracun()
    {
        if (_sveKartice.Count == 0) { Poruka = "Nema kartica za obračun."; return; }

        if (System.Windows.MessageBox.Show(
                $"Izračunati amortizaciju za sve kartice ({_sveKartice.Count})?\n\n" +
                "Ažurira AMORT, ISP, SAD (MRS) i AMORT2, ISP2, SAD2 (PP).",
                "Obračun amortizacije",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        int obradjeno = 0;
        foreach (var k in _sveKartice)
        {
            var nacinob = k.ExtraPolja.TryGetValue("NACINOB", out var n) ? n?.ToString()?.Trim() : null;
            if (!string.IsNullOrWhiteSpace(nacinob)) continue;

            // MRS — linearno na osnovu SAD0
            if (k.StopaOt > 0 && k.Sad0 > 0)
            {
                var amort = Math.Round(k.Sad0 * k.StopaOt / 100m, 2);
                if (amort > k.Sad0) amort = k.Sad0;
                k.ExtraPolja["AMORT"] = amort;
                k.ExtraPolja["ISP"]   = amort;
                k.ExtraPolja["SAD"]   = k.Sad0 - amort;
            }

            // PP — linearno na osnovu SAD02
            var stopaot2 = XDec(k, "STOPAOT2");
            var sad02    = XDec(k, "SAD02");
            if (stopaot2 > 0 && sad02 > 0)
            {
                var amort2 = Math.Round(sad02 * stopaot2 / 100m, 2);
                if (amort2 > sad02) amort2 = sad02;
                k.ExtraPolja["AMORT2"] = amort2;
                k.ExtraPolja["ISP2"]   = amort2;
                k.ExtraPolja["SAD2"]   = sad02 - amort2;
            }
            obradjeno++;
        }
        Poruka = $"Obračun završen — {obradjeno} kartica obrađeno. Kliknite Sačuvaj.";
        if (obradjeno > 0) _izmijenjeno = true;
    }

    [RelayCommand]
    private void PoaObrazac()
    {
        var (_, periodDo) = ProcitajPeriod();
        var vm = new OsPoaIzvestajViewModel(_sveKartice, OsPoaIzvestajViewModel.TipIzvestaja.PoaObrazac, periodDo);
        new OsPoaIzvestajWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void EvidencijaPoa()
    {
        var vm = new OsPoaIzvestajViewModel(_sveKartice, OsPoaIzvestajViewModel.TipIzvestaja.EvidencijaPoa, null);
        new OsPoaIzvestajWindow(vm).ShowDialog();
    }

    private static decimal XDec(OsKartica k, string polje)
        => OsSaldoViewModel.DajDec(k, polje);

    private void SelektujKarticu(OsKartica kartica)
    {
        if (!Kartice.Contains(kartica))
        {
            FilterText = string.Empty;
            PrimeniFiIlter();
        }

        IzabranaKartica = kartica;
    }

    private (DateTime? od, DateTime? @do) ProcitajPeriod()
    {
        var path = DbfPutanja("ospodaci.dbf");
        if (path == null) return (null, null);

        try
        {
            var reader = new SimpleDbfReader(path);
            foreach (var r in reader.Zapisi())
                return (r.DajDate("EDAT0"), r.DajDate("EDAT1"));
        }
        catch
        {
            // Izvestaj moze i bez perioda.
        }

        return (null, null);
    }

    private OsPopisFilterData UcitajPopisKriterijume()
    {
        var path = DbfPutanja("ospopis.dbf");
        if (path == null) return new OsPopisFilterData();

        try
        {
            var reader = new SimpleDbfReader(path);
            foreach (var r in reader.Zapisi())
            {
                var mtr = r.DajInt("MTR");
                return new OsPopisFilterData
                {
                    Mesto = r.DajString("MESTO").Trim(),
                    Mtr = mtr < 1 ? 0 : mtr,
                    Konto = r.DajString("KONTO").Trim(),
                    Ag = r.DajString("AG").Trim(),
                    AgPod = r.DajString("AGPOD").Trim(),
                    Grupa = r.DajString("GRUPA").Trim()
                };
            }
        }
        catch
        {
            // Ako ne mozemo da procitamo kriterijume, nastavljamo sa praznim.
        }

        return new OsPopisFilterData();
    }

    private void SacuvajPopisKriterijume(OsPopisFilterData data)
    {
        var path = DbfPutanja("ospopis.dbf");
        if (path == null) return;

        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            var redovi = CitajSveRedove(path);

            if (redovi.Count == 0)
                redovi.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));

            var r = redovi[0];
            r["MESTO"] = data.Mesto?.Trim() ?? string.Empty;
            r["MTR"] = data.Mtr < 1 ? 0 : data.Mtr;
            r["KONTO"] = data.Konto?.Trim() ?? string.Empty;
            r["AG"] = data.Ag?.Trim() ?? string.Empty;
            r["AGPOD"] = data.AgPod?.Trim() ?? string.Empty;
            r["GRUPA"] = data.Grupa?.Trim() ?? string.Empty;

            DbfTableWriter.WriteTable(path, schema, redovi,
                (red, f) => red.TryGetValue(f, out var v) ? v : null);
        }
        catch
        {
            // Ne prekidamo rad ako ne uspe upis kriterijuma.
        }
    }

    private static IEnumerable<OsKartica> PrimeniPopisFilter(IEnumerable<OsKartica> kartice, OsPopisFilterData data)
    {
        var mmesto = (data.Mesto ?? string.Empty).Trim();
        var mmtr = data.Mtr < 1 ? 0 : data.Mtr;
        var mkonto = (data.Konto ?? string.Empty).Trim();
        var mag = (data.Ag ?? string.Empty).Trim();
        var magpod = (data.AgPod ?? string.Empty).Trim();
        var mgrupa = (data.Grupa ?? string.Empty).Trim();

        return kartice.Where(k =>
            (string.IsNullOrWhiteSpace(mmesto) || string.Equals((k.Mesto ?? string.Empty).Trim(), mmesto, StringComparison.OrdinalIgnoreCase)) &&
            (mmtr == 0 || DajExtraInt(k, "MTR") == mmtr) &&
            (string.IsNullOrWhiteSpace(mag) || string.Equals((k.Ag ?? string.Empty).Trim(), mag, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(magpod) || string.Equals((k.AgPod ?? string.Empty).Trim(), magpod, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(mkonto) || (k.Konto ?? string.Empty).TrimEnd().StartsWith(mkonto, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(mgrupa) || string.Equals(DajExtra(k, "GRUPA"), mgrupa, StringComparison.OrdinalIgnoreCase)));
    }

    private List<Dictionary<string, object?>> CitajSveRedove(string path)
    {
        var reader = new SimpleDbfReader(path);
        var svi = new List<Dictionary<string, object?>>();

        foreach (var r in reader.Zapisi())
        {
            var red = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in reader.Fields)
            {
                red[f.Name] = f.Type switch
                {
                    'D' => (object?)r.DajDate(f.Name),
                    'N' or 'F' => r.DajDecimal(f.Name),
                    'L' => r.DajBool(f.Name),
                    _ => r.DajString(f.Name)
                };
            }
            svi.Add(red);
        }

        return svi;
    }

    private static string DajExtra(OsKartica k, string polje)
    {
        if (!k.ExtraPolja.TryGetValue(polje, out var v) || v is null)
            return string.Empty;

        return Convert.ToString(v)?.Trim() ?? string.Empty;
    }

    private static int DajExtraInt(OsKartica k, string polje)
    {
        if (!k.ExtraPolja.TryGetValue(polje, out var v) || v is null)
            return 0;

        return v switch
        {
            int i => i,
            long l => (int)l,
            decimal d => (int)d,
            double db => (int)db,
            float f => (int)f,
            _ when int.TryParse(Convert.ToString(v), out var n) => n,
            _ => 0
        };
    }


    private string? DbfPutanja(string ime)
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder)) return null;

        var hit = NadjiDbf(folder, ime);
        if (hit != null) return hit;

        var root = FinWorkspaceResolver.NormalizeRootPath(folder);
        hit = NadjiDbf(Path.Combine(root, "data00"), ime);
        if (hit != null) return hit;

        return NadjiDbf(Path.Combine(AppContext.BaseDirectory, "data00"), ime);
    }

    private static string? NadjiDbf(string folder, string ime)
    {
        if (!Directory.Exists(folder)) return null;
        foreach (var naziv in new[] { ime, ime.ToUpperInvariant() })
        {
            var p = Path.Combine(folder, naziv);
            if (File.Exists(p)) return p;
        }
        return Directory.GetFiles(folder, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Equals(ime, StringComparison.OrdinalIgnoreCase));
    }
}
