using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;
using Serilog;
using System.Collections.ObjectModel;

namespace OsnovnaSredstva.ViewModels;

public partial class OsKarticeViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext<OsKarticeViewModel>();
    private readonly AppState _appState;

    private List<OsKartica> _sveKartice = [];

    [ObservableProperty] private ObservableCollection<OsKartica> _kartice = [];
    [ObservableProperty] private OsKartica? _izabranaKartica;
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private string _filterText = "";

    private bool _izmijenjeno;
    public bool ImaNeSnimljenih => _izmijenjeno;

    public string NazivIzabrane =>
        IzabranaKartica == null ? "" : $"{IzabranaKartica.Osifra}   {IzabranaKartica.Naz}";

    partial void OnIzabranaKarticaChanged(OsKartica? value) =>
        OnPropertyChanged(nameof(NazivIzabrane));

    private static readonly HashSet<string> PoznataPolja = new(StringComparer.OrdinalIgnoreCase)
    {
        "OSIFRA","NAZ","DATNAB","BRNAL","KONTO","VRSTA",
        "AG","AGPOD","INVBROJ","MESTO","NAB0","ISP0","SAD0",
        "KOM","CENA","STOPAOT","OSNOVKOR","IZVOR","PRENETO","IDBR"
    };

    public OsKarticeViewModel(AppState appState)
    {
        _appState = appState;
        Ucitaj();
    }

    partial void OnFilterTextChanged(string value) => PrimeniFIlter();

    private void PrimeniFIlter()
    {
        var f = FilterText?.Trim() ?? "";
        if (string.IsNullOrEmpty(f))
        {
            Kartice = new ObservableCollection<OsKartica>(_sveKartice);
        }
        else
        {
            var fl = f.ToLowerInvariant();
            Kartice = new ObservableCollection<OsKartica>(
                _sveKartice.Where(k =>
                    (k.Osifra ?? "").ToLowerInvariant().Contains(fl) ||
                    (k.Naz    ?? "").ToLowerInvariant().Contains(fl)));
        }
    }

    private void Ucitaj()
    {
        var path = DbfPutanja("os0.dbf");
        if (path == null) { Kartice = []; Poruka = "os0.dbf nije pronađen u folderu firme."; return; }

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
                            'D'      => (object?)r.DajDate(field.Name),
                            'N' or 'F' => r.DajDecimal(field.Name),
                            'L'      => r.DajBool(field.Name),
                            _        => r.DajString(field.Name)
                        };
                    }
                }

                stavke.Add(k);
            }

            _sveKartice = stavke;
            PrimeniFIlter();
            Poruka = $"Učitano {_sveKartice.Count} kartica.";
            _log.Debug("OsKartice — učitano {Count} kartica iz {File}", _sveKartice.Count, path);
            _izmijenjeno = false;
        }
        catch (Exception ex)
        {
            _sveKartice = [];
            Kartice = [];
            Poruka = $"Greška: {ex.Message}";
            _log.Error(ex, "OsKartice — greška pri učitavanju");
        }
    }

    [RelayCommand]
    private void Dodaj()
    {
        var sledecaSifra = IzracunajSledecuSifru();
        var nova = new OsKartica { Osifra = sledecaSifra };
        _sveKartice.Add(nova);
        PrimeniFIlter();
        IzabranaKartica = nova;
        Poruka = $"Nova kartica dodata sa sifrom {sledecaSifra}. Unesite podatke i kliknite Sačuvaj.";
        _log.Information("OsKartice — dodana nova kartica {Sifra}", sledecaSifra);
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void Obrisi()
    {
        if (IzabranaKartica == null)
        {
            Poruka = "Nije izabrana kartica.";
            return;
        }

        var opis = $"'{IzabranaKartica.Osifra}' — {IzabranaKartica.Naz}";
        if (System.Windows.MessageBox.Show(
                $"Brisanje kartice OS: {opis}\n\nDa li ste sigurni?",
                "Potvrda brisanja",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        _sveKartice.Remove(IzabranaKartica);
        PrimeniFIlter();
        Poruka = $"Kartica {opis} obrisana. Kliknite Sačuvaj.";
        _log.Information("OsKartice — obrisana kartica {Opis}", opis);
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void Kartica()
    {
        if (IzabranaKartica == null)
        {
            Poruka = "Nije izabrana kartica. Kliknite red pa dugme KARTICA.";
            return;
        }

        var path = DbfPutanja("os0.dbf");
        if (path == null) { Poruka = "os0.dbf nije pronađen."; return; }

        try
        {
            var vm = new OsKarticaKarticaViewModel(IzabranaKartica, _appState);
            var win = new OsKarticaKarticaWindow(vm);
            if (win.ShowDialog() == true)
            {
                Poruka = "Kartica je ažurirana. Kliknite Sačuvaj za trajni upis u DBF.";
                _izmijenjeno = true;
            }
        }
        catch (Exception ex) { Poruka = $"Kartica: {ex.Message}"; }
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        var path = DbfPutanja("os0.dbf");
        if (path == null) { Poruka = "os0.dbf nije pronađen."; return; }

        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, _sveKartice,
                (k, f) => f.ToUpperInvariant() switch
                {
                    "OSIFRA"  => (object?)k.Osifra,
                    "NAZ"     => k.Naz,
                    "DATNAB"  => k.DatNab,
                    "BRNAL"   => k.BrNal,
                    "KONTO"   => k.Konto,
                    "VRSTA"   => k.Vrsta,
                    "AG"      => k.Ag,
                    "AGPOD"   => k.AgPod,
                    "INVBROJ" => k.InvBroj,
                    "MESTO"   => k.Mesto,
                    "NAB0"    => k.Nab0,
                    "ISP0"    => k.Isp0,
                    "SAD0"    => k.Sad0,
                    "KOM"     => k.Kom,
                    "CENA"    => k.Cena,
                    "STOPAOT" => k.StopaOt,
                    "OSNOVKOR"=> k.OsnovKor,
                    "IZVOR"   => k.Izvor,
                    "PRENETO" => k.Preneto,
                    "IDBR"    => (object?)k.IDBr,
                    _         => k.ExtraPolja.TryGetValue(f, out var v) ? v : null
                });

            Poruka = $"Kartice sačuvane ({_sveKartice.Count} zapisa).";
            _log.Information("OsKartice — sačuvano {Count} kartica u {File}", _sveKartice.Count, path);
            _izmijenjeno = false;
        }
        catch (Exception ex)
        {
            Poruka = $"Greška: {ex.Message}";
            _log.Error(ex, "OsKartice — greška pri snimanju");
        }
    }

    [RelayCommand]
    private void Sort()
    {
        _sveKartice = [.. _sveKartice.OrderBy(k => k.Osifra)];
        PrimeniFIlter();
        Poruka = "Sortirano po sifri OS.";
    }

    [RelayCommand]
    private void BrisanjePraznina()
    {
        var count = _sveKartice.RemoveAll(k => string.IsNullOrWhiteSpace(k.Osifra));
        PrimeniFIlter();
        Poruka = count > 0
            ? $"Obrisano {count} praznih kartica. Kliknite Sačuvaj."
            : "Nema praznih kartica.";
        if (count > 0) _izmijenjeno = true;
    }

    [RelayCommand]
    private void IzvozTxt1()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Izvoz u TXT",
            Filter = "Text fajlovi (*.txt)|*.txt|Svi fajlovi (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = "os0_izvoz.txt"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            using var sw = new System.IO.StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8);
            sw.WriteLine($"{"Šifra",-6} {"Naziv",-40} {"Konto",-10} {"Mesto",-8} {"AG",-4} {"NAB0",14} {"ISP0",14} {"SAD0",14} {"STOPAOT",9}");
            sw.WriteLine(new string('-', 121));
            foreach (var k in _sveKartice)
                sw.WriteLine($"{(k.Osifra ?? ""),-6} {(k.Naz ?? ""),-40} {(k.Konto ?? ""),-10} {(k.Mesto ?? ""),-8} {(k.Ag ?? ""),-4} {k.Nab0,14:N2} {k.Isp0,14:N2} {k.Sad0,14:N2} {k.StopaOt,9:N3}");
            Poruka = $"Izvoz TXT završen: {dlg.FileName} ({_sveKartice.Count} zapisa).";
        }
        catch (Exception ex) { Poruka = $"Greška izvoza: {ex.Message}"; }
    }

    [RelayCommand]
    private void IzvozExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Izvoz u CSV (Excel)",
            Filter = "CSV fajlovi (*.csv)|*.csv|Svi fajlovi (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "os0_izvoz.csv"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            using var sw = new System.IO.StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8);
            sw.WriteLine("Šifra;Naziv;DatNab;BrNal;Konto;Vrsta;AG;AgPod;InvBroj;Mesto;NAB0;ISP0;SAD0;Kom;Cena;StopaOt;OsnovKor;Izvor;Preneto");
            foreach (var k in _sveKartice)
                sw.WriteLine(string.Join(";",
                    k.Osifra?.Trim() ?? "",
                    k.Naz?.Trim() ?? "",
                    k.DatNab?.ToString("dd.MM.yyyy") ?? "",
                    k.BrNal?.Trim() ?? "",
                    k.Konto?.Trim() ?? "",
                    k.Vrsta?.Trim() ?? "",
                    k.Ag?.Trim() ?? "",
                    k.AgPod?.Trim() ?? "",
                    k.InvBroj?.Trim() ?? "",
                    k.Mesto?.Trim() ?? "",
                    k.Nab0.ToString("N2"),
                    k.Isp0.ToString("N2"),
                    k.Sad0.ToString("N2"),
                    k.Kom.ToString("N2"),
                    k.Cena.ToString("N2"),
                    k.StopaOt.ToString("N3"),
                    k.OsnovKor?.Trim() ?? "",
                    k.Izvor?.Trim() ?? "",
                    k.Preneto?.Trim() ?? ""));
            Poruka = $"Izvoz CSV završen: {dlg.FileName} ({_sveKartice.Count} zapisa).";
        }
        catch (Exception ex) { Poruka = $"Greška izvoza: {ex.Message}"; }
    }

    [RelayCommand]
    private void IzvozExcel2()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Izvoz u CSV (Excel) — sve kolone",
            Filter = "CSV fajlovi (*.csv)|*.csv|Svi fajlovi (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "os0_izvoz2.csv"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var extraKljucevi = _sveKartice
                .SelectMany(k => k.ExtraPolja.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            using var sw = new System.IO.StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8);
            sw.WriteLine("Šifra;Naziv;DatNab;BrNal;Konto;Vrsta;AG;AgPod;InvBroj;Mesto;NAB0;ISP0;SAD0;Kom;Cena;StopaOt;OsnovKor;Izvor;Preneto;"
                       + string.Join(";", extraKljucevi));
            foreach (var k in _sveKartice)
            {
                var bazni = string.Join(";",
                    k.Osifra?.Trim() ?? "",
                    k.Naz?.Trim() ?? "",
                    k.DatNab?.ToString("dd.MM.yyyy") ?? "",
                    k.BrNal?.Trim() ?? "",
                    k.Konto?.Trim() ?? "",
                    k.Vrsta?.Trim() ?? "",
                    k.Ag?.Trim() ?? "",
                    k.AgPod?.Trim() ?? "",
                    k.InvBroj?.Trim() ?? "",
                    k.Mesto?.Trim() ?? "",
                    k.Nab0.ToString("N2"),
                    k.Isp0.ToString("N2"),
                    k.Sad0.ToString("N2"),
                    k.Kom.ToString("N2"),
                    k.Cena.ToString("N2"),
                    k.StopaOt.ToString("N3"),
                    k.OsnovKor?.Trim() ?? "",
                    k.Izvor?.Trim() ?? "",
                    k.Preneto?.Trim() ?? "");
                var extra = string.Join(";", extraKljucevi.Select(ek =>
                    k.ExtraPolja.TryGetValue(ek, out var v) ? v?.ToString() ?? "" : ""));
                sw.WriteLine(bazni + ";" + extra);
            }
            Poruka = $"Izvoz CSV (sve kolone) završen: {dlg.FileName} ({_sveKartice.Count} zapisa).";
        }
        catch (Exception ex) { Poruka = $"Greška izvoza: {ex.Message}"; }
    }

    [RelayCommand]
    private void GrupeZaAmortizaciju()
    {
        var vm = new OsSifarnikViewModel(_appState);
        vm.AktivniTab = 1;
        new OsSifarnikWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PodgrupeZaAmortizaciju()
    {
        var vm = new OsSifarnikViewModel(_appState);
        vm.AktivniTab = 2;
        new OsSifarnikWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void Nepokretnosti()
    {
        var filtriran = _sveKartice
            .Where(k => k.ExtraPolja.TryGetValue("POVRSINA", out var p) &&
                        p is decimal d && d > 0m)
            .ToList();
        if (filtriran.Count == 0)
            filtriran = _sveKartice.Where(k => k.ExtraPolja.ContainsKey("POVRSINA")).ToList();
        if (filtriran.Count == 0) { Poruka = "Nema nepokretnosti (polje POVRSINA nije pronađeno)."; return; }
        var vm = OsMrsViewModel.MrsPregled(filtriran);
        vm.Naslov = $"NEPOKRETNOSTI — {filtriran.Count} zapisa";
        new OsMrsWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PopisnaLista()
    {
        var vm = OsSaldoViewModel.PoKontu(_sveKartice);
        new OsSaldoWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Prvi()
    {
        if (Kartice.Count > 0) IzabranaKartica = Kartice[0];
    }

    [RelayCommand]
    private void Zadnji()
    {
        if (Kartice.Count > 0) IzabranaKartica = Kartice[^1];
    }

    [RelayCommand]
    private void Dole()
    {
        if (IzabranaKartica == null || Kartice.Count == 0) return;
        var idx = Kartice.IndexOf(IzabranaKartica);
        if (idx < Kartice.Count - 1) IzabranaKartica = Kartice[idx + 1];
    }

    [RelayCommand]
    private void Gore()
    {
        if (IzabranaKartica == null || Kartice.Count == 0) return;
        var idx = Kartice.IndexOf(IzabranaKartica);
        if (idx > 0) IzabranaKartica = Kartice[idx - 1];
    }

    private string? DbfPutanja(string ime) => DbfHelper.NadjiDbf(_appState, ime);

    private string IzracunajSledecuSifru()
    {
        var max = 0;
        foreach (var kartica in _sveKartice)
        {
            var raw = (kartica.Osifra ?? string.Empty).Trim();
            if (int.TryParse(raw, out var broj) && broj > max)
                max = broj;
        }
        // FoxPro: STR(MOSIFRA,4,0) = space-padded right-aligned 4-char string
        return (max + 1).ToString().PadLeft(4);
    }
}
