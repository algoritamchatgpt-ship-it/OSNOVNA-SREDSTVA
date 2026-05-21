using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using Serilog;

namespace OsnovnaSredstva.ViewModels;

public partial class OsPodaciViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext<OsPodaciViewModel>();
    private readonly AppState _appState;
    private string? _ospodaciPath;
    private string? _osPath;

    [ObservableProperty] private DateTime? _eDat0;
    [ObservableProperty] private DateTime? _eDat1;
    [ObservableProperty] private int _eMes;
    [ObservableProperty] private string _brNal = "";
    [ObservableProperty] private DateTime? _datDok;
    [ObservableProperty] private string _konAm = "";
    [ObservableProperty] private string _poruka = "";

    public OsPodaciViewModel(AppState appState)
    {
        _appState = appState;
        Ucitaj();
    }

    [RelayCommand]
    private void Ucitaj()
    {
        _ospodaciPath = DbfPutanja("ospodaci.dbf");
        _osPath = DbfPutanja("os.dbf");

        if (_ospodaciPath == null)
        {
            Poruka = "ospodaci.dbf nije pronađen u folderu firme.";
            return;
        }

        try
        {
            var zapisi = CitajSveRedove(_ospodaciPath);
            if (zapisi.Count == 0)
            {
                EDat0 = null;
                EDat1 = null;
                EMes = 0;
                BrNal = string.Empty;
                DatDok = null;
                KonAm = string.Empty;
                Poruka = "ospodaci.dbf je prazan. Popunite podatke i kliknite UNOS PODATAKA.";
                return;
            }

            var r = zapisi[0];
            EDat0 = DajDate(r, "EDAT0");
            EDat1 = DajDate(r, "EDAT1");
            EMes = (int)DajDec(r, "EMES");
            BrNal = DajStr(r, "BRNAL");
            DatDok = DajDate(r, "DATDOK");
            KonAm = DajStr(r, "KONAM");
            Poruka = "Podaci učitani.";
            _log.Debug("ospodaci.dbf: učitani podaci (EDAT0={Edat0}, EDAT1={Edat1})", EDat0, EDat1);
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri učitavanju: {ex.Message}";
            _log.Error(ex, "Greška pri učitavanju ospodaci.dbf");
        }
    }

    [RelayCommand]
    private void UnosPodataka()
    {
        if (_ospodaciPath == null)
        {
            Poruka = "ospodaci.dbf nije pronađen.";
            return;
        }

        if (_osPath == null)
        {
            Poruka = "os.dbf nije pronađen.";
            return;
        }

        if (!EDat0.HasValue || !EDat1.HasValue)
        {
            Poruka = "Unesite početni i zadnji datum.";
            return;
        }

        var medat0 = EDat0.Value.Date;
        var medat1 = EDat1.Value.Date;
        if (medat1 < medat0)
        {
            Poruka = "Zadnji datum ne može biti manji od početnog.";
            return;
        }

        try
        {
            SacuvajOspodaci(medat0, medat1);
            var rezultat = AzurirajOs(medat0, medat1);
            Poruka = $"UNOS PODATAKA završen. Ažurirano {rezultat.azurirano} zapisa, obrisano {rezultat.obrisano}.";
            _log.Information("UNOS PODATAKA: ažurirano {Azurirano}, obrisano {Obrisano}; period {Od}–{Do}", rezultat.azurirano, rezultat.obrisano, medat0, medat1);
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri obradi: {ex.Message}";
            _log.Error(ex, "Greška pri UNOS PODATAKA (period {Od}–{Do})", medat0, medat1);
        }
    }

    private void SacuvajOspodaci(DateTime medat0, DateTime medat1)
    {
        if (_ospodaciPath == null) return;

        var schema = DbfTableWriter.LoadSchema(_ospodaciPath);
        var zapisi = CitajSveRedove(_ospodaciPath);

        if (zapisi.Count == 0)
            zapisi.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));

        var r = zapisi[0];
        r["EDAT0"] = medat0;
        r["EDAT1"] = medat1;
        r["EMES"] = EMes;
        r["BRNAL"] = BrNal?.Trim() ?? string.Empty;
        r["DATDOK"] = DatDok;
        r["KONAM"] = KonAm?.Trim() ?? string.Empty;

        DbfTableWriter.WriteTable(_ospodaciPath, schema, zapisi,
            (red, f) => red.TryGetValue(f, out var v) ? v : null);
    }

    private (int azurirano, int obrisano) AzurirajOs(DateTime medat0, DateTime medat1)
    {
        if (_osPath == null) return (0, 0);

        var schema = DbfTableWriter.LoadSchema(_osPath);
        var zapisi = CitajSveRedove(_osPath);
        var novi = new List<Dictionary<string, object?>>(zapisi.Count);
        var obrisano = 0;

        foreach (var r in zapisi)
        {
            var datProd = DajDate(r, "DATPROD");
            if (datProd.HasValue && datProd.Value.Year < medat0.Year)
            {
                obrisano++;
                continue;
            }

            var datNab = DajDate(r, "DATNAB");
            if (!datNab.HasValue)
            {
                datNab = medat0.AddDays(-1);
                r["DATNAB"] = datNab.Value;
            }

            if (!datProd.HasValue)
            {
                datProd = medat1;
                r["DATPROD"] = datProd.Value;
            }

            var datum0 = datNab.Value < medat0 ? medat0 : datNab.Value;
            var datum1 = datProd.Value < medat1 ? datProd.Value : medat1;

            r["DATUM0"] = datum0;
            r["DATUM1"] = datum1;

            if (datum0 == medat0 && datum1 == medat1)
            {
                r["BRMES"] = EMes;
            }
            else
            {
                var memes2 = datum1.Month - datum0.Month;
                r["BRMES"] = memes2 > 0 ? memes2 : 0;
            }

            novi.Add(r);
        }

        DbfTableWriter.WriteTable(_osPath, schema, novi,
            (red, f) => red.TryGetValue(f, out var v) ? v : null);

        return (novi.Count, obrisano);
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

    private string? DbfPutanja(string ime) => DbfHelper.NadjiDbf(_appState, ime);

    private static string DajStr(IDictionary<string, object?> r, string polje)
        => r.TryGetValue(polje, out var v) ? Convert.ToString(v)?.Trim() ?? string.Empty : string.Empty;

    private static decimal DajDec(IDictionary<string, object?> r, string polje)
    {
        if (!r.TryGetValue(polje, out var v) || v == null) return 0m;
        return v switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            _ => decimal.TryParse(Convert.ToString(v), out var parsed) ? parsed : 0m
        };
    }

    private static DateTime? DajDate(IDictionary<string, object?> r, string polje)
    {
        if (!r.TryGetValue(polje, out var v) || v == null) return null;
        return v switch
        {
            DateTime dt => dt,
            string s when DateTime.TryParse(s, out var dt) => dt,
            _ => null
        };
    }
}
