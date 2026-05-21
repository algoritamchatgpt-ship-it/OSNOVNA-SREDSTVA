using CommunityToolkit.Mvvm.ComponentModel;
using OsnovnaSredstva.Models;
using System.Collections.ObjectModel;

namespace OsnovnaSredstva.ViewModels;

public partial class OsPoaIzvestajViewModel : ObservableObject
{
    public enum TipIzvestaja
    {
        PoaObrazac,
        EvidencijaPoa
    }

    [ObservableProperty] private string _naslov = "";
    [ObservableProperty] private string _poruka = "";
    public ObservableCollection<OsPoaIzvestajRed> Stavke { get; } = [];

    public OsPoaIzvestajViewModel(IEnumerable<OsKartica> kartice, TipIzvestaja tip, DateTime? periodDo)
    {
        Naslov = tip == TipIzvestaja.PoaObrazac
            ? "POA OBRAZAC"
            : "EVIDENCIJA POA";

        var src = kartice ?? Enumerable.Empty<OsKartica>();
        IEnumerable<OsKartica> filtrirano = tip switch
        {
            TipIzvestaja.EvidencijaPoa => src.Where(k =>
                !string.IsNullOrWhiteSpace(DajStr(k, "NACINOB"))),

            TipIzvestaja.PoaObrazac => src.Where(k =>
            {
                var nacinob = DajStr(k, "NACINOB");
                if (!nacinob.Equals("POA", StringComparison.OrdinalIgnoreCase)) return false;

                var datProd = DajDate(k, "DATPROD");
                if (!datProd.HasValue) return false;
                if (datProd.Value <= new DateTime(2019, 1, 1)) return false;
                if (periodDo.HasValue && datProd.Value >= periodDo.Value.Date) return false;
                return true;
            }),

            _ => src
        };

        foreach (var k in filtrirano.OrderBy(k => k.Osifra, StringComparer.OrdinalIgnoreCase))
        {
            Stavke.Add(new OsPoaIzvestajRed
            {
                Osifra = k.Osifra?.Trim() ?? "",
                Naziv = k.Naz?.Trim() ?? "",
                InvBroj = k.InvBroj?.Trim() ?? "",
                DatProd = DajDate(k, "DATPROD"),
                NacinOb = DajStr(k, "NACINOB"),
                Sad = DajDec(k, "SAD"),
                Sad2 = DajDec(k, "SAD2"),
                Pam = DajDec(k, "PAM"),
                Ram = DajDec(k, "RAM"),
                Obezvredj = DajDec(k, "OBEZVREDJ")
            });
        }

        Poruka = $"Ukupno {Stavke.Count} zapisa.";
    }

    private static string DajStr(OsKartica k, string polje)
        => k.ExtraPolja.TryGetValue(polje, out var v)
            ? Convert.ToString(v)?.Trim() ?? string.Empty
            : string.Empty;

    private static decimal DajDec(OsKartica k, string polje)
        => OsSaldoViewModel.DajDec(k, polje);

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void Stampaj()
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
                FontFamily  = new System.Windows.Media.FontFamily("Courier New"),
                FontSize    = 9
            };
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run(Naslov))
                { FontSize = 13, FontWeight = System.Windows.FontWeights.Bold });

            var tbl = new System.Windows.Documents.Table { CellSpacing = 0 };
            foreach (var w in new double[] { 65, 200, 100, 85, 75, 90, 90, 90, 90, 100 })
                tbl.Columns.Add(new System.Windows.Documents.TableColumn
                    { Width = new System.Windows.GridLength(w) });

            var rg = new System.Windows.Documents.TableRowGroup();
            tbl.RowGroups.Add(rg);

            void DodajRed(string[] c, bool header = false)
            {
                var row = new System.Windows.Documents.TableRow();
                foreach (var cel in c)
                {
                    var para = new System.Windows.Documents.Paragraph(
                        new System.Windows.Documents.Run(cel))
                        { Padding = new System.Windows.Thickness(2, 1, 2, 1), TextAlignment = System.Windows.TextAlignment.Right };
                    if (header) { para.FontWeight = System.Windows.FontWeights.Bold; para.TextAlignment = System.Windows.TextAlignment.Left; }
                    row.Cells.Add(new System.Windows.Documents.TableCell(para)
                    {
                        BorderBrush = System.Windows.Media.Brushes.Black,
                        BorderThickness = new System.Windows.Thickness(0, 0, 0, header ? 1 : 0)
                    });
                }
                rg.Rows.Add(row);
            }

            DodajRed(["Osifra", "Naziv", "InvBroj", "DatProd", "NacinOb", "Sad", "Sad2", "PAM", "RAM", "Obezvredj"], header: true);
            foreach (var s in Stavke)
                DodajRed([s.Osifra, s.Naziv, s.InvBroj,
                    s.DatProd?.ToString("dd.MM.yyyy") ?? "",
                    s.NacinOb,
                    s.Sad.ToString("N2"), s.Sad2.ToString("N2"),
                    s.Pam.ToString("N2"), s.Ram.ToString("N2"), s.Obezvredj.ToString("N2")]);

            doc.Blocks.Add(tbl);
            var paginator = ((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator;
            dlg.PrintDocument(paginator, Naslov);
            Poruka = $"Štampanje završeno ({Stavke.Count} redova).";
        }
        catch (Exception ex) { Poruka = $"Greška štampanja: {ex.Message}"; }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void IzveziCsv()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Izvoz u CSV",
            Filter = "CSV (*.csv)|*.csv|Svi fajlovi (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = Naslov.Replace(" ", "_") + ".csv"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            using var sw = new System.IO.StreamWriter(dlg.FileName, false, new System.Text.UTF8Encoding(true));
            sw.WriteLine("Osifra;Naziv;InvBroj;DatProd;NacinOb;Sad;Sad2;PAM;RAM;Obezvredj");
            foreach (var s in Stavke)
                sw.WriteLine($"{s.Osifra};{s.Naziv};{s.InvBroj};{s.DatProd?.ToString("dd.MM.yyyy") ?? ""};{s.NacinOb};{s.Sad:N2};{s.Sad2:N2};{s.Pam:N2};{s.Ram:N2};{s.Obezvredj:N2}");
            Poruka = $"CSV izvoz završen: {dlg.FileName} ({Stavke.Count} redova).";
        }
        catch (Exception ex) { Poruka = $"Greška izvoza: {ex.Message}"; }
    }

    private static DateTime? DajDate(OsKartica k, string polje)
    {
        if (!k.ExtraPolja.TryGetValue(polje, out var v) || v == null) return null;
        return v switch
        {
            DateTime dt => dt,
            string s when DateTime.TryParse(s, out var dt) => dt,
            _ => null
        };
    }
}

public class OsPoaIzvestajRed
{
    public string Osifra { get; set; } = "";
    public string Naziv { get; set; } = "";
    public string InvBroj { get; set; } = "";
    public DateTime? DatProd { get; set; }
    public string NacinOb { get; set; } = "";
    public decimal Sad { get; set; }
    public decimal Sad2 { get; set; }
    public decimal Pam { get; set; }
    public decimal Ram { get; set; }
    public decimal Obezvredj { get; set; }
}
