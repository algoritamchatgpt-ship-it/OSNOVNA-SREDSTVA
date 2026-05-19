using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using System.Collections.ObjectModel;
using System.IO;

namespace OsnovnaSredstva.ViewModels;

public partial class OsObrazacOaViewModel : ObservableObject
{
    private readonly AppState _appState;
    private List<OsOaStavka> _sveStavke = [];

    [ObservableProperty] private ObservableCollection<OsOaStavka> _stavke = [];
    [ObservableProperty] private OsOaStavka? _izabranaStavka;
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private string _infoIzabrane = "";

    partial void OnIzabranaStavkaChanged(OsOaStavka? value)
        => InfoIzabrane = value == null ? "" : $"AG: {value.Ag}   Agstopa: {value.AgStopa:N3}   Neotpis: {value.Neotpis:N2}   Amort2: {value.Amort2:N2}   Sad2: {value.Sad2:N2}";

    public OsObrazacOaViewModel(AppState appState)
    {
        _appState = appState;
        Ucitaj();
    }

    private void Ucitaj()
    {
        var path = DbfPutanja("osoa.dbf");
        if (path == null) { Stavke = []; Poruka = "osoa.dbf nije pronadjen u folderu firme."; return; }

        try
        {
            var reader = new SimpleDbfReader(path);
            var lista = new List<OsOaStavka>();

            foreach (var r in reader.Zapisi())
            {
                lista.Add(new OsOaStavka
                {
                    Ag      = r.DajString("AG"),
                    Pocetno = r.DajDecimal("POCETNO"),
                    Nabavka = r.DajDecimal("NABAVKA"),
                    Prodaja = r.DajDecimal("PRODAJA"),
                    Neotpis = r.DajDecimal("NEOTPIS"),
                    AgStopa = r.DajDecimal("AGSTOPA"),
                    Amort2  = r.DajDecimal("AMORT2"),
                    Sad2    = r.DajDecimal("SAD2"),
                    Preneto = r.DajString("PRENETO"),
                    Numred  = (int)r.DajDecimal("NUMRED"),
                    IDBr    = (int)r.DajDecimal("IDBR"),
                });
            }

            _sveStavke = lista;
            Stavke = new ObservableCollection<OsOaStavka>(_sveStavke);
            IzabranaStavka = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {_sveStavke.Count} zapisa iz osoa.dbf.";
        }
        catch (Exception ex)
        {
            _sveStavke = [];
            Stavke = [];
            Poruka = $"Greska: {ex.Message}";
        }
    }

    [RelayCommand] private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Dodaj()
    {
        var max = _sveStavke.Select(s => s.IDBr).DefaultIfEmpty(0).Max();
        var nova = new OsOaStavka { IDBr = max + 1 };
        _sveStavke.Add(nova);
        Stavke = new ObservableCollection<OsOaStavka>(_sveStavke);
        IzabranaStavka = nova;
        Poruka = "Novi red dodan. Unesite podatke i kliknite Sacuvaj.";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        var path = DbfPutanja("osoa.dbf");
        if (path == null) { Poruka = "osoa.dbf nije pronadjen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, _sveStavke,
                (s, f) => f.ToUpperInvariant() switch
                {
                    "AG"      => (object?)s.Ag,
                    "POCETNO" => s.Pocetno,
                    "NABAVKA" => s.Nabavka,
                    "PRODAJA" => s.Prodaja,
                    "NEOTPIS" => s.Neotpis,
                    "AGSTOPA" => s.AgStopa,
                    "AMORT2"  => s.Amort2,
                    "SAD2"    => s.Sad2,
                    "PRENETO" => s.Preneto,
                    "NUMRED"  => (object?)s.Numred,
                    "IDBR"    => (object?)s.IDBr,
                    _         => null
                });
            Poruka = $"Sacuvano ({_sveStavke.Count} zapisa).";
        }
        catch (Exception ex) { Poruka = $"Greska pri snimanju: {ex.Message}"; }
    }

    [RelayCommand]
    private void UcitajGrupe()
    {
        var path = DbfPutanjaZaGrupe("osag.dbf");
        if (path == null) { Poruka = "osag.dbf nije pronadjen."; return; }

        try
        {
            var reader = new SimpleDbfReader(path);
            var lista = new List<OsOaStavka>();
            var idbr = 1;

            foreach (var r in reader.Zapisi())
            {
                var ag = r.DajString("AG").Trim();
                if (ag == "1") continue;   // FoxPro: DELETE ALL FOR AG='1'

                var postojeca = _sveStavke.FirstOrDefault(s => s.Ag.Trim() == ag);
                lista.Add(new OsOaStavka
                {
                    Ag      = ag,
                    AgStopa = r.DajDecimal("AGSTOPA"),
                    Pocetno = postojeca?.Pocetno ?? 0m,
                    Nabavka = postojeca?.Nabavka ?? 0m,
                    Prodaja = postojeca?.Prodaja ?? 0m,
                    Neotpis = postojeca?.Neotpis ?? 0m,
                    Amort2  = postojeca?.Amort2  ?? 0m,
                    Sad2    = postojeca?.Sad2    ?? 0m,
                    Preneto = postojeca?.Preneto ?? "",
                    Numred  = idbr,
                    IDBr    = idbr++,
                });
            }

            _sveStavke = lista;
            Stavke = new ObservableCollection<OsOaStavka>(_sveStavke);
            IzabranaStavka = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {_sveStavke.Count} amortizacionih grupa.";
        }
        catch (Exception ex) { Poruka = $"Greska pri ucitavanju grupa: {ex.Message}"; }
    }

    [RelayCommand]
    private void UcitajPodatke()
    {
        if (_sveStavke.Count == 0) { Poruka = "Nema grupa — prvo pokrenite UCITAJ GRUPE."; return; }

        var osPath = DbfPutanja("os.dbf");
        if (osPath == null) { Poruka = "os.dbf nije pronadjen u folderu firme."; return; }

        try
        {
            // Čitamo period datume iz ospodaci.dbf ako postoji
            DateTime? edat0 = null, edat1 = null;
            var podaciPath = DbfPutanja("ospodaci.dbf");
            if (podaciPath != null)
            {
                var podaciReader = new SimpleDbfReader(podaciPath);
                foreach (var r in podaciReader.Zapisi())
                {
                    edat0 = r.DajDate("EDAT0");
                    edat1 = r.DajDate("EDAT1");
                    break;
                }
            }

            // Učitavamo OS zapise
            var osReader = new SimpleDbfReader(osPath);
            var osZapisi = osReader.Zapisi().ToList();

            // Nuliramo sve sume
            foreach (var s in _sveStavke)
            {
                s.Pocetno = 0m;
                s.Nabavka = 0m;
                s.Prodaja = 0m;
                s.Neotpis = 0m;
                s.Amort2  = 0m;
                s.Sad2    = 0m;
            }

            // Agregiramo po AG (filter: EMPTY(NACINOB))
            foreach (var r in osZapisi)
            {
                var nacinob = r.DajString("NACINOB").Trim();
                if (!string.IsNullOrEmpty(nacinob)) continue;

                var ag  = r.DajString("AG").Trim();
                var s   = _sveStavke.FirstOrDefault(x => x.Ag.Trim() == ag);
                if (s == null) continue;

                var datum0 = r.DajDate("DATUM0");
                var datum1 = r.DajDate("DATUM1");
                var sad02  = r.DajDecimal("SAD02");
                var nab02  = r.DajDecimal("NAB02");
                var amort2 = r.DajDecimal("AMORT2");
                var sad2   = r.DajDecimal("SAD2");

                if (edat0.HasValue && datum0.HasValue && datum0.Value == edat0.Value)
                    s.Pocetno += sad02;

                if (edat0.HasValue && datum0.HasValue && datum0.Value > edat0.Value)
                    s.Nabavka += nab02;

                if (edat1.HasValue && datum1.HasValue && datum1.Value < edat1.Value)
                    s.Prodaja += sad02;

                s.Neotpis += sad02;
                s.Amort2  += amort2;
                s.Sad2    += sad2;
            }

            Stavke = new ObservableCollection<OsOaStavka>(_sveStavke);
            var datInfo = edat0.HasValue ? $" (period od {edat0:dd.MM.yyyy} do {edat1:dd.MM.yyyy})" : " (bez filtera datuma)";
            Poruka = $"Podaci ucitani iz {Path.GetFileName(osPath)}{datInfo}.";
        }
        catch (Exception ex) { Poruka = $"Greska pri ucitavanju podataka: {ex.Message}"; }
    }

    [RelayCommand]
    private void Preracun()
    {
        foreach (var s in _sveStavke)
        {
            s.Neotpis = s.Pocetno + s.Nabavka - s.Prodaja;
            s.Amort2  = Math.Round(s.Neotpis * s.AgStopa / 100m, 0);
            s.Sad2    = s.Neotpis - s.Amort2;
        }
        Stavke = new ObservableCollection<OsOaStavka>(_sveStavke);
        Poruka = "Preracun izvrseno.";
    }

    [RelayCommand]
    private void BrisanjePraznina()
    {
        _sveStavke.RemoveAll(s => string.IsNullOrWhiteSpace(s.Ag));
        Stavke = new ObservableCollection<OsOaStavka>(_sveStavke);
        Poruka = "Praznine obrisane.";
    }

    [RelayCommand] private void PreglediOa()  => IspisiIzvestaj("OA Obrazac");
    [RelayCommand] private void PreglediOa1() => IspisiIzvestaj("OA-1 Obrazac");

    private void IspisiIzvestaj(string naslov)
    {
        var dlg = new System.Windows.Controls.PrintDialog();
        if (dlg.ShowDialog() != true) return;
        try
        {
            var doc = new System.Windows.Documents.FlowDocument
            {
                PageWidth   = dlg.PrintableAreaWidth,
                PageHeight  = dlg.PrintableAreaHeight,
                ColumnWidth = dlg.PrintableAreaWidth,
                FontFamily  = new System.Windows.Media.FontFamily("Tahoma"),
                FontSize    = 10
            };

            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run(naslov))
                { FontSize = 14, FontWeight = System.Windows.FontWeights.Bold });

            var tbl = new System.Windows.Documents.Table { CellSpacing = 0 };
            foreach (var w in new double[] { 60, 110, 110, 110, 110, 80, 110, 110 })
                tbl.Columns.Add(new System.Windows.Documents.TableColumn
                    { Width = new System.Windows.GridLength(w) });

            var rg = new System.Windows.Documents.TableRowGroup();
            tbl.RowGroups.Add(rg);

            void DodajRed(string[] celije, bool header = false)
            {
                var row = new System.Windows.Documents.TableRow();
                foreach (var c in celije)
                {
                    var para = new System.Windows.Documents.Paragraph(
                        new System.Windows.Documents.Run(c))
                        { Padding = new System.Windows.Thickness(2, 1, 2, 1) };
                    if (header) para.FontWeight = System.Windows.FontWeights.Bold;
                    row.Cells.Add(new System.Windows.Documents.TableCell(para)
                    {
                        BorderBrush     = System.Windows.Media.Brushes.Black,
                        BorderThickness = new System.Windows.Thickness(0, 0, 0, header ? 1 : 0)
                    });
                }
                rg.Rows.Add(row);
            }

            DodajRed(["AG", "Pocetno", "Nabavka", "Prodaja", "Neotpis", "AgStopa%", "Amort2", "Sad2"], header: true);
            foreach (var s in _sveStavke)
                DodajRed([s.Ag, s.Pocetno.ToString("N2"), s.Nabavka.ToString("N2"),
                    s.Prodaja.ToString("N2"), s.Neotpis.ToString("N2"),
                    s.AgStopa.ToString("N3"), s.Amort2.ToString("N2"), s.Sad2.ToString("N2")]);

            doc.Blocks.Add(tbl);
            var paginator = ((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator;
            dlg.PrintDocument(paginator, naslov);
            Poruka = $"Štampanje '{naslov}' poslano na štampač.";
        }
        catch (Exception ex) { Poruka = $"Greška štampanja: {ex.Message}"; }
    }

    [RelayCommand] private void Prvi()   { if (Stavke.Count > 0) IzabranaStavka = Stavke[0]; }
    [RelayCommand] private void Zadnji() { if (Stavke.Count > 0) IzabranaStavka = Stavke[^1]; }
    [RelayCommand] private void Dole()
    {
        var sel = IzabranaStavka;
        if (sel == null || Stavke.Count == 0) return;
        var idx = Stavke.IndexOf(sel);
        if (idx < Stavke.Count - 1) IzabranaStavka = Stavke[idx + 1];
    }
    [RelayCommand] private void Gore()
    {
        var sel = IzabranaStavka;
        if (sel == null || Stavke.Count == 0) return;
        var idx = Stavke.IndexOf(sel);
        if (idx > 0) IzabranaStavka = Stavke[idx - 1];
    }

    private string? DbfPutanjaZaGrupe(string ime)
    {
        var kandidatiFoldera = new List<string>();

        static bool IstaPutanja(string left, string right)
            => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        void DodajFolder(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return;
            if (kandidatiFoldera.Any(x => IstaPutanja(x, folder))) return;
            kandidatiFoldera.Add(folder);
        }

        var folderFirme = _appState.AktivnaFirma?.FolderPath;
        if (!string.IsNullOrWhiteSpace(folderFirme))
        {
            var root = FinWorkspaceResolver.NormalizeRootPath(folderFirme);
            DodajFolder(Path.Combine(root, "data00"));
            DodajFolder(Path.Combine(folderFirme, "data00"));
            DodajFolder(folderFirme);
            DodajFolder(root);
            DodajFolder(Path.Combine(root, "data01"));
        }

        DodajFolder(Path.Combine(Directory.GetCurrentDirectory(), "data00"));
        DodajFolder(Directory.GetCurrentDirectory());
        DodajFolder(Path.Combine(AppContext.BaseDirectory, "data00"));
        DodajFolder(AppContext.BaseDirectory);

        string? prviPronadjen = null;
        foreach (var folder in kandidatiFoldera)
        {
            var putanja = PronadjiDbfUFolderu(folder, ime);
            if (string.IsNullOrWhiteSpace(putanja)) continue;

            prviPronadjen ??= putanja;
            if (ImaZapisaDbf(putanja)) return putanja;
        }

        return prviPronadjen;
    }

    private string? DbfPutanjaAny(params string[] imena)
    {
        string? prviPronadjen = null;
        foreach (var ime in imena)
        {
            var putanja = DbfPutanja(ime);
            if (string.IsNullOrWhiteSpace(putanja)) continue;

            prviPronadjen ??= putanja;
            if (ImaZapisaDbf(putanja)) return putanja;
        }
        return prviPronadjen;
    }

    private string? DbfPutanja(string ime)
    {
        var kandidatiFoldera = new List<string>();

        static bool IstaPutanja(string left, string right)
            => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        void DodajFolder(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return;
            if (kandidatiFoldera.Any(x => IstaPutanja(x, folder))) return;
            kandidatiFoldera.Add(folder);
        }

        var folderFirme = _appState.AktivnaFirma?.FolderPath;
        if (!string.IsNullOrWhiteSpace(folderFirme))
        {
            DodajFolder(folderFirme);
            DodajFolder(Path.Combine(folderFirme, "data00"));

            var root = FinWorkspaceResolver.NormalizeRootPath(folderFirme);
            DodajFolder(root);
            DodajFolder(Path.Combine(root, "data00"));
            DodajFolder(Path.Combine(root, "data01"));
        }

        DodajFolder(Directory.GetCurrentDirectory());
        DodajFolder(Path.Combine(Directory.GetCurrentDirectory(), "data00"));
        DodajFolder(AppContext.BaseDirectory);
        DodajFolder(Path.Combine(AppContext.BaseDirectory, "data00"));

        foreach (var folder in kandidatiFoldera)
        {
            var putanja = PronadjiDbfUFolderu(folder, ime);
            if (!string.IsNullOrWhiteSpace(putanja)) return putanja;
        }

        return null;
    }

    private static string? PronadjiDbfUFolderu(string folder, string ime)
    {
        if (!Directory.Exists(folder)) return null;

        foreach (var naziv in new[] { ime, ime.ToUpperInvariant(), ime.ToLowerInvariant() })
        {
            var putanja = Path.Combine(folder, naziv);
            if (File.Exists(putanja)) return putanja;
        }

        return Directory.GetFiles(folder, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Equals(ime, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ImaZapisaDbf(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var header = new byte[12];
            if (fs.Read(header, 0, header.Length) < header.Length) return false;
            var recordCount = BitConverter.ToInt32(header, 4);
            return recordCount > 0;
        }
        catch
        {
            return false;
        }
    }
}
