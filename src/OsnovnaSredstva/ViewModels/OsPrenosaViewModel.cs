using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using System.Collections.ObjectModel;
using System.IO;

namespace OsnovnaSredstva.ViewModels;

public partial class OsPrenosaViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PokrenuiPrenosuCommand))]
    private bool _uToku = false;

    [ObservableProperty] private string _trenutniPeriod = "";
    [ObservableProperty] private int _novaGodina;
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _uspjesno = false;

    public ObservableCollection<string> Log { get; } = [];

    public OsPrenosaViewModel(AppState appState)
    {
        _appState = appState;
        _novaGodina = appState.AktivnaGodina + 1;
        UcitajTrenutnePeriod();
    }

    [RelayCommand] private void PovecajGodinu() => NovaGodina = Math.Min(NovaGodina + 1, 2099);
    [RelayCommand] private void SmanjuGodinu()  => NovaGodina = Math.Max(NovaGodina - 1, 2000);

    private bool MozePokrenuti() => !UToku;

    [RelayCommand(CanExecute = nameof(MozePokrenuti))]
    private async Task PokrenuiPrenosu()
    {
        Log.Clear();
        Uspjesno = false;
        UToku = true;
        StatusPoruka = "Prenos u toku...";

        try
        {
            var noviEdat0 = new DateTime(NovaGodina, 1, 1);
            var noviEdat1 = new DateTime(NovaGodina, 12, 31);

            await Task.Run(() =>
            {
                // 1. ospodaci.dbf — ažuriranje perioda
                var podaciPath = DbfPutanja("ospodaci.dbf");
                if (podaciPath != null)
                {
                    AzurirajOspodaci(podaciPath, noviEdat0, noviEdat1);
                    DodajLog($"✓ ospodaci.dbf — period: {noviEdat0:dd.MM.yyyy} do {noviEdat1:dd.MM.yyyy}");
                }
                else
                {
                    DodajLog("Napomena: ospodaci.dbf nije pronađen.");
                }

                // 2. os.dbf — prenos SAD2 → SAD02 za novu godinu
                var osPath = DbfPutanja("os.dbf");
                if (osPath != null)
                {
                    var n = AzurirajOs(osPath, noviEdat0);
                    DodajLog($"✓ os.dbf — ažurirano {n} zapisa (SAD2 → SAD02, novi DATUM0).");
                }
                else
                {
                    DodajLog("Napomena: os.dbf nije pronađen.");
                }
            });

            _appState.AktivnaGodina = NovaGodina;
            TrenutniPeriod = $"01.01.{NovaGodina} — 31.12.{NovaGodina}";
            DodajLog("─────────────────────────────────");
            DodajLog($"Prenos u {NovaGodina}. godinu završen.");
            StatusPoruka = $"Prenos uspješno završen. Aktivna godina: {NovaGodina}.";
            Uspjesno = true;
        }
        catch (Exception ex)
        {
            DodajLog($"GREŠKA: {ex.Message}");
            StatusPoruka = $"Prenos nije završen — {ex.Message}";
        }
        finally
        {
            UToku = false;
        }
    }

    private void UcitajTrenutnePeriod()
    {
        var path = DbfPutanja("ospodaci.dbf");
        if (path == null) { TrenutniPeriod = "ospodaci.dbf nije pronađen"; return; }

        try
        {
            var reader = new SimpleDbfReader(path);
            foreach (var r in reader.Zapisi())
            {
                var edat0 = r.DajDate("EDAT0");
                var edat1 = r.DajDate("EDAT1");
                TrenutniPeriod = edat0.HasValue && edat1.HasValue
                    ? $"{edat0:dd.MM.yyyy} — {edat1:dd.MM.yyyy}"
                    : "Period nije definisan";
                return;
            }
            TrenutniPeriod = "ospodaci.dbf je prazan";
        }
        catch (Exception ex) { TrenutniPeriod = $"Greška: {ex.Message}"; }
    }

    private static void AzurirajOspodaci(string path, DateTime edat0, DateTime edat1)
    {
        var schema = DbfTableWriter.LoadSchema(path);
        var reader = new SimpleDbfReader(path);
        var zapisi = reader.Zapisi().ToList();
        if (zapisi.Count == 0) return;

        var prviZapis = zapisi[0];
        var red = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in reader.Fields)
        {
            red[f.Name] = f.Type switch
            {
                'D'      => (object?)prviZapis.DajDate(f.Name),
                'N' or 'F' => prviZapis.DajDecimal(f.Name),
                'L'      => prviZapis.DajBool(f.Name),
                _        => prviZapis.DajString(f.Name)
            };
        }
        red["EDAT0"] = edat0;
        red["EDAT1"] = edat1;

        DbfTableWriter.WriteTable(path, schema, new[] { red },
            (r, f) => r.TryGetValue(f, out var v) ? v : null);
    }

    private static int AzurirajOs(string path, DateTime noviDatum0)
    {
        var schema = DbfTableWriter.LoadSchema(path);
        var reader = new SimpleDbfReader(path);
        var fields = reader.Fields;
        var sviRedovi = new List<Dictionary<string, object?>>();
        var count = 0;

        foreach (var r in reader.Zapisi())
        {
            var red = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in fields)
            {
                red[f.Name] = f.Type switch
                {
                    'D'        => (object?)r.DajDate(f.Name),
                    'N' or 'F' => r.DajDecimal(f.Name),
                    'L'        => r.DajBool(f.Name),
                    _          => r.DajString(f.Name)
                };
            }

            // ── PP polja (poreska propisa) ──────────────────────────────────────
            var sad2  = r.DajDecimal("SAD2");
            var isp2  = r.DajDecimal("ISP2");

            // Početne PP vrijednosti nove godine = završne vrijednosti prethodne
            red["SAD02"]  = sad2;    // poč. sadašnja PP  = završ. sadašnja PP
            red["ISP02"]  = isp2;    // poč. ispravka PP  = završ. ispravka PP
            red["NAB02"]  = 0m;      // poč. nabavka PP   = 0 (nema nabavki na dan 1.1.)
            // Tekući period PP — resetuj amortizaciju i nabavku; sadašnja = poč.
            red["AMORT2"] = 0m;
            red["NAB2"]   = 0m;
            red["ISP2"]   = isp2;    // tekuća ispravka PP kreće od poč. vrijednosti
            red["SAD2"]   = sad2;    // tekuća sadašnja PP kreće od poč. vrijednosti

            // ── MRS polja (međunarodni računovodstveni standardi) ───────────────
            var nab0 = r.DajDecimal("NAB0");
            var isp0 = r.DajDecimal("ISP0");
            var nab  = r.DajDecimal("NAB");
            var isp  = r.DajDecimal("ISP");

            // Ukupne kumulativne vrijednosti na kraju prethodne godine
            var noviNab0 = nab0 + nab;       // ukupna nabavna MRS
            var noviIsp0 = isp0 + isp;       // ukupna ispravka MRS
            var noviSad0 = noviNab0 - noviIsp0;

            red["NAB0"]   = noviNab0;        // poč. nabavna MRS  = kumulativ
            red["ISP0"]   = noviIsp0;        // poč. ispravka MRS = kumulativ
            red["SAD0"]   = noviSad0;        // poč. sadašnja MRS = NAB0 - ISP0
            // Tekući period MRS — resetuj na 0, tekuće = početne
            red["NAB"]    = 0m;
            red["ISP"]    = 0m;
            red["SAD"]    = noviSad0;        // tekuća sadašnja MRS kreće od poč.
            red["AMORT"]  = 0m;

            // ── Datum ────────────────────────────────────────────────────────────
            red["DATUM0"] = noviDatum0;

            count++;
            sviRedovi.Add(red);
        }

        DbfTableWriter.WriteTable(path, schema, sviRedovi,
            (r, f) => r.TryGetValue(f, out var v) ? v : null);

        return count;
    }

    private void DodajLog(string poruka)
    {
        App.Current.Dispatcher.Invoke(() => Log.Add(poruka));
        StatusPoruka = poruka;
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
