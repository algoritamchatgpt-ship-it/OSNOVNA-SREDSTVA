using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Utilities;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace OsnovnaSredstva.ViewModels;

public record TabelaInfo(string Naziv, string Putanja, int BrojZapisa);

public partial class IzvozTabelaViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly IPutanjaService _putanjaService;
    private List<TabelaInfo> _sveTabele = [];

    [ObservableProperty] private ObservableCollection<TabelaInfo> _prikazaneTabele = [];
    [ObservableProperty] private TabelaInfo? _izabranaTabela;
    [ObservableProperty] private string _pretraga = "";
    [ObservableProperty] private string _izvozPutanja = "";
    [ObservableProperty] private bool _xmlFormat = true;
    [ObservableProperty] private bool _csvFormat = false;
    [ObservableProperty] private bool _pdfFormat = false;
    [ObservableProperty] private bool _dbfFormat = false;
    [ObservableProperty] private string _poruka = "Izaberite tabelu iz liste i kliknite IZVEZI.";
    [ObservableProperty] private bool _radi;

    public IzvozTabelaViewModel(AppState appState, IPutanjaService putanjaService)
    {
        _appState = appState;
        _putanjaService = putanjaService;
        IzvozPutanja = putanjaService.DajIzvozPutanju() ?? "";
        UcitajTabele();
    }

    partial void OnPretragaChanged(string value) => Filtriraj();

    partial void OnXmlFormatChanged(bool value) { if (value) { CsvFormat = false; PdfFormat = false; DbfFormat = false; } }
    partial void OnCsvFormatChanged(bool value) { if (value) { XmlFormat = false; PdfFormat = false; DbfFormat = false; } }
    partial void OnPdfFormatChanged(bool value) { if (value) { XmlFormat = false; CsvFormat = false; DbfFormat = false; } }
    partial void OnDbfFormatChanged(bool value) { if (value) { XmlFormat = false; CsvFormat = false; PdfFormat = false; } }

    partial void OnRadiChanged(bool value) => IzveziCommand.NotifyCanExecuteChanged();

    private void UcitajTabele()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        _sveTabele = Directory.GetFiles(folder, "*.dbf", SearchOption.TopDirectoryOnly)
            .OrderBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
            .Select(p => new TabelaInfo(
                Path.GetFileNameWithoutExtension(p).ToUpperInvariant(),
                p,
                CitajBrojZapisa(p)))
            .ToList();

        Poruka = _sveTabele.Count == 0
            ? "Nije pronađen nijedan DBF fajl u folderu firme."
            : $"Pronađeno {_sveTabele.Count} tabela. Izaberite jednu i kliknite IZVEZI.";

        Filtriraj();
    }

    private void Filtriraj()
    {
        var tekst = Pretraga.Trim().ToUpperInvariant();
        var filtrirano = string.IsNullOrEmpty(tekst)
            ? _sveTabele
            : _sveTabele.Where(t => t.Naziv.Contains(tekst));
        PrikazaneTabele = new ObservableCollection<TabelaInfo>(filtrirano);
    }

    [RelayCommand]
    private void OdaberiPutanju()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Izaberite folder za izvoz tabele"
        };
        if (!string.IsNullOrWhiteSpace(IzvozPutanja) && Directory.Exists(IzvozPutanja))
            dialog.InitialDirectory = IzvozPutanja;

        if (dialog.ShowDialog() == true)
        {
            IzvozPutanja = dialog.FolderName;
            _putanjaService.SnimiIzvozPutanju(IzvozPutanja);
        }
    }

    [RelayCommand(CanExecute = nameof(MozeIzvesti))]
    private async Task IzveziAsync()
    {
        if (IzabranaTabela == null) { Poruka = "Nije izabrana tabela."; return; }
        if (string.IsNullOrWhiteSpace(IzvozPutanja)) { Poruka = "Nije izabrana putanja za izvoz."; return; }

        try
        {
            if (!Directory.Exists(IzvozPutanja))
                Directory.CreateDirectory(IzvozPutanja);
        }
        catch
        {
            Poruka = $"Nije moguće kreirati folder: {IzvozPutanja}";
            return;
        }

        Radi = true;
        Poruka = $"Izvoz tabele {IzabranaTabela!.Naziv} u toku...";

        try
        {
            if (XmlFormat) await IzveziXmlAsync(IzabranaTabela, IzvozPutanja);
            else if (CsvFormat) await IzveziCsvAsync(IzabranaTabela, IzvozPutanja);
            else if (PdfFormat) await IzveziPdfAsync(IzabranaTabela, IzvozPutanja);
            else await IzveziDbfAsync(IzabranaTabela, IzvozPutanja);

            _putanjaService.SnimiIzvozPutanju(IzvozPutanja);

            var format = XmlFormat ? "XML" : CsvFormat ? "CSV" : PdfFormat ? "PDF" : "DBF";
            Poruka = $"Tabela {IzabranaTabela.Naziv} uspešno izvezena kao {format} u: {IzvozPutanja}";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri izvozu: {ex.Message}";
        }
        finally { Radi = false; }
    }

    private bool MozeIzvesti() => !Radi;

    private static async Task IzveziXmlAsync(TabelaInfo tabela, string folder)
    {
        var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(tabela.Putanja));
        var xdoc = new XDocument(
            new XElement("Tabela",
                new XAttribute("naziv", tabela.Naziv),
                new XAttribute("datumIzvoza", DateTime.Today.ToString("yyyy-MM-dd")),
                new XAttribute("brojZapisa", zapisi.Count),
                zapisi.Select(r => new XElement("Zapis",
                    r.Select(kv => new XElement(
                        string.IsNullOrEmpty(kv.Key) ? "Polje" : kv.Key,
                        kv.Value?.ToString() ?? ""))))));

        var putanja = Path.Combine(folder, tabela.Naziv + ".xml");
        await Task.Run(() => xdoc.Save(putanja));
    }

    private static async Task IzveziDbfAsync(TabelaInfo tabela, string folder)
    {
        await Task.Run(() =>
        {
            var destDbf = Path.Combine(folder, Path.GetFileName(tabela.Putanja));
            File.Copy(tabela.Putanja, destDbf, overwrite: true);

            foreach (var ext in new[] { ".cdx", ".CDX", ".fpt", ".FPT", ".mdx", ".MDX" })
            {
                var src = Path.ChangeExtension(tabela.Putanja, ext);
                if (File.Exists(src))
                    File.Copy(src, Path.Combine(folder, Path.GetFileName(src)), overwrite: true);
            }
        });
    }

    private static async Task IzveziPdfAsync(TabelaInfo tabela, string folder)
    {
        var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(tabela.Putanja));
        var putanja = Path.Combine(folder, tabela.Naziv + ".pdf");

        await Task.Run(() =>
        {
            var naslov = $"{tabela.Naziv} — {zapisi.Count} zapisa — {DateTime.Today:dd.MM.yyyy}";
            var lines = new List<string>();

            if (zapisi.Count == 0)
            {
                lines.Add("(nema zapisa)");
                SimplePdfWriter.WriteTextPdf(putanja, naslov, lines);
                return;
            }

            var kolone = zapisi[0].Keys.ToList();
            var sirine = kolone.Select(k => Math.Min(18, Math.Max(k.Length,
                zapisi.Take(500).Select(r => r.TryGetValue(k, out var v) ? (v?.ToString() ?? "").Length : 0)
                      .DefaultIfEmpty(0).Max()))).ToList();

            const int maxLineWidth = 90;
            var grupe = new List<List<int>>();
            var tekucaGrupa = new List<int>();
            int tekucaSirina = 0;

            for (int i = 0; i < kolone.Count; i++)
            {
                int potrebno = sirine[i] + 1;
                if (tekucaGrupa.Count > 0 && tekucaSirina + potrebno > maxLineWidth)
                {
                    grupe.Add(tekucaGrupa);
                    tekucaGrupa = [];
                    tekucaSirina = 0;
                }
                tekucaGrupa.Add(i);
                tekucaSirina += potrebno;
            }
            if (tekucaGrupa.Count > 0) grupe.Add(tekucaGrupa);

            for (int g = 0; g < grupe.Count; g++)
            {
                var grupa = grupe[g];
                if (g > 0) { lines.Add(""); lines.Add($"--- kolone {g + 1}/{grupe.Count} ---"); lines.Add(""); }

                var header = string.Concat(grupa.Select(ci => kolone[ci].PadRight(sirine[ci] + 1))).TrimEnd();
                lines.Add(header);
                lines.Add(new string('-', Math.Min(header.Length, maxLineWidth)));

                foreach (var zapis in zapisi)
                {
                    var row = string.Concat(grupa.Select(ci =>
                    {
                        var val = zapis.TryGetValue(kolone[ci], out var v) ? (v?.ToString() ?? "") : "";
                        if (val.Length > sirine[ci]) val = val[..sirine[ci]];
                        return val.PadRight(sirine[ci] + 1);
                    })).TrimEnd();
                    lines.Add(row);
                }
            }

            SimplePdfWriter.WriteTextPdf(putanja, naslov, lines);
        });
    }

    private static async Task IzveziCsvAsync(TabelaInfo tabela, string folder)
    {
        var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(tabela.Putanja));
        var putanja = Path.Combine(folder, tabela.Naziv + ".csv");

        await Task.Run(() =>
        {
            using var sw = new StreamWriter(putanja, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            if (zapisi.Count == 0) return;

            var kolone = zapisi[0].Keys.ToList();
            sw.WriteLine(string.Join(";", kolone.Select(CsvEscape)));

            foreach (var zapis in zapisi)
                sw.WriteLine(string.Join(";", kolone.Select(k => CsvEscape(zapis.TryGetValue(k, out var v) ? v?.ToString() ?? "" : ""))));
        });
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private static int CitajBrojZapisa(string putanja)
    {
        try
        {
            using var fs = new FileStream(putanja, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var br = new BinaryReader(fs);
            br.ReadByte();
            br.ReadBytes(3);
            return (int)br.ReadUInt32();
        }
        catch { return 0; }
    }
}
