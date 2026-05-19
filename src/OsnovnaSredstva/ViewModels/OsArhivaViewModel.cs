using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;
using System.Collections.ObjectModel;
using System.IO;

namespace OsnovnaSredstva.ViewModels;

public partial class OsArhivaViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly string _dbfIme;

    private List<OsKartica> _sveKartice = [];

    [ObservableProperty] private ObservableCollection<OsKartica> _kartice = [];
    [ObservableProperty] private OsKartica? _izabranaKartica;
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private string _filterText = "";

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

    public OsArhivaViewModel(AppState appState, string dbfIme = "osa.dbf")
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
        if (path == null) { Kartice = []; Poruka = $"{_dbfIme} nije pronadjen u folderu firme."; return; }

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
            PrimeniFiIlter();
            Poruka = $"Ucitano {_sveKartice.Count} zapisa iz {_dbfIme}.";
        }
        catch (Exception ex)
        {
            _sveKartice = [];
            Kartice = [];
            Poruka = $"Greska: {ex.Message}";
        }
    }

    [RelayCommand] private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Dodaj()
    {
        var max = _sveKartice.Select(k => k.IDBr).DefaultIfEmpty(0).Max();
        var nova = new OsKartica { IDBr = max + 1, Preneto = "N" };
        _sveKartice.Add(nova);
        PrimeniFiIlter();
        IzabranaKartica = nova;
        Poruka = "Novi red dodan. Unesite podatke i kliknite Sacuvaj.";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        var path = DbfPutanja(_dbfIme);
        if (path == null) { Poruka = $"{_dbfIme} nije pronadjen."; return; }
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
            Poruka = $"Sacuvano ({_sveKartice.Count} zapisa).";
        }
        catch (Exception ex) { Poruka = $"Greska: {ex.Message}"; }
    }

    [RelayCommand]
    private void Sort()
    {
        _sveKartice = [.. _sveKartice.OrderBy(k => k.Osifra)];
        PrimeniFiIlter();
        Poruka = "Sortirano po sifri.";
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
        var input = Microsoft.VisualBasic.Interaction.InputBox("Unesite šifru OS:", "Traženje po šifri", "");
        if (string.IsNullOrWhiteSpace(input)) return;
        var trag = input.Trim();
        var nadjeno = Kartice.FirstOrDefault(k =>
            (k.Osifra ?? "").Trim().Equals(trag, StringComparison.OrdinalIgnoreCase));
        if (nadjeno != null) { IzabranaKartica = nadjeno; Poruka = $"Pronađena: {nadjeno.Osifra?.Trim()} — {nadjeno.Naz}"; }
        else Poruka = $"Šifra '{trag}' nije pronađena.";
    }

    [RelayCommand]
    private void TrazenjeInventarnogBroja()
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox("Unesite inventarski broj:", "Traženje po inv. broju", "");
        if (string.IsNullOrWhiteSpace(input)) return;
        var trag = input.Trim();
        var nadjeno = Kartice.FirstOrDefault(k =>
            (k.InvBroj ?? "").Trim().Equals(trag, StringComparison.OrdinalIgnoreCase));
        if (nadjeno != null) { IzabranaKartica = nadjeno; Poruka = $"Pronađena: {nadjeno.Osifra?.Trim()} — {nadjeno.Naz} (InvBroj: {nadjeno.InvBroj?.Trim()})"; }
        else Poruka = $"Inventarski broj '{trag}' nije pronađen.";
    }

    [RelayCommand] private void PregledKartica()
    {
        if (IzabranaKartica == null) { Poruka = "Nije izabran red."; return; }
        var vm = new OsKarticaKarticaViewModel(IzabranaKartica, _appState);
        var win = new OsKarticaKarticaWindow(vm);
        if (win.ShowDialog() == true) Poruka = "Kartica azurirana. Kliknite Sacuvaj.";
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
        var vm = OsSaldoViewModel.PoMestu(_sveKartice);
        new OsSaldoWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PregledMrs()
    {
        var vm = OsMrsViewModel.MrsPregled(_sveKartice);
        new OsMrsWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PreglPoreskaStara()
    {
        var vm = OsMrsViewModel.PoreskaStara(_sveKartice);
        new OsMrsWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PreglPoreskaNova()
    {
        var vm = OsMrsViewModel.PoreskaNova(_sveKartice);
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
        var path = DbfPutanja("ospodaci.dbf");
        if (path == null) { Poruka = "ospodaci.dbf nije pronađen u folderu firme."; return; }
        try
        {
            var reader = new SimpleDbfReader(path);
            foreach (var r in reader.Zapisi())
            {
                var edat0 = r.DajDate("EDAT0");
                var edat1 = r.DajDate("EDAT1");
                Poruka = $"Trenutni period: " +
                         $"{(edat0.HasValue ? edat0.Value.ToString("dd.MM.yyyy") : "—")} — " +
                         $"{(edat1.HasValue ? edat1.Value.ToString("dd.MM.yyyy") : "—")}";
                return;
            }
            Poruka = "ospodaci.dbf je prazan.";
        }
        catch (Exception ex) { Poruka = $"Greška čitanja ospodaci: {ex.Message}"; }
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

            if (k.StopaOt > 0 && k.Sad0 > 0)
            {
                var amort = Math.Round(k.Sad0 * k.StopaOt / 100m, 2);
                if (amort > k.Sad0) amort = k.Sad0;
                k.ExtraPolja["AMORT"] = amort;
                k.ExtraPolja["ISP"]   = amort;
                k.ExtraPolja["SAD"]   = k.Sad0 - amort;
            }

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
    }

    [RelayCommand]
    private void PoaObrazac()
    {
        var vm = new OsObrazacOaViewModel(_appState);
        new OsObrazacOaWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void EvidencijaPoa()
    {
        var vm = new OsObrazacOaViewModel(_appState);
        new OsObrazacOaWindow(vm).ShowDialog();
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
            // Saldo izvestaji rade i bez perioda.
        }

        return (null, null);
    }

    private static decimal XDec(OsKartica k, string polje)
        => OsSaldoViewModel.DajDec(k, polje);

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
