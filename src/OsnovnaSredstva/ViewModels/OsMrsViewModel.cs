using CommunityToolkit.Mvvm.ComponentModel;
using OsnovnaSredstva.Models;
using System.Collections.ObjectModel;

namespace OsnovnaSredstva.ViewModels;

public partial class OsMrsViewModel : ObservableObject
{
    public enum TipPregledaEnum { Mrs, PoreskaStara, PoreskaNova }

    [ObservableProperty] private string _naslov = "";
    [ObservableProperty] private string _poruka = "";

    public TipPregledaEnum TipPregledaVm { get; private set; }

    public bool JeMrs => TipPregledaVm == TipPregledaEnum.Mrs;
    public bool JePoreskaStara => TipPregledaVm == TipPregledaEnum.PoreskaStara;
    public bool JePoreskaNova => TipPregledaVm == TipPregledaEnum.PoreskaNova;

    public ObservableCollection<OsMrsRedak> Stavke { get; } = [];

    public static OsMrsViewModel MrsPregled(IEnumerable<OsKartica> kartice, bool skraceni = false)
    {
        var vm = new OsMrsViewModel { TipPregledaVm = TipPregledaEnum.Mrs };
        vm.Naslov = skraceni
            ? "PREGLED MRS - Skraceni"
            : "PREGLED MRS - Osnovna sredstva";
        vm.Ucitaj(kartice);
        return vm;
    }

    public static OsMrsViewModel PoreskaStara(IEnumerable<OsKartica> kartice)
    {
        var vm = new OsMrsViewModel { TipPregledaVm = TipPregledaEnum.PoreskaStara };
        vm.Naslov = "PREGLED PORESKE - Pocetne vrednosti (PP)";
        vm.Ucitaj(kartice);
        return vm;
    }

    public static OsMrsViewModel PoreskaNova(IEnumerable<OsKartica> kartice)
    {
        var vm = new OsMrsViewModel { TipPregledaVm = TipPregledaEnum.PoreskaNova };
        vm.Naslov = "PREGLED PORESKE - Tekuce vrednosti (PP)";
        vm.Ucitaj(kartice);
        return vm;
    }

    private void Ucitaj(IEnumerable<OsKartica> kartice)
    {
        foreach (var k in kartice)
        {
            Stavke.Add(new OsMrsRedak
            {
                Sifra = k.Osifra?.Trim() ?? "",
                Naziv = k.Naz?.Trim() ?? "",
                Konto = k.Konto?.Trim() ?? "",
                Mesto = k.Mesto?.Trim() ?? "",
                Ag = k.Ag?.Trim() ?? "",
                Nab0 = k.Nab0,
                Isp0 = k.Isp0,
                Sad0 = k.Sad0,
                StopaOt = k.StopaOt,
                Amort = D(k, "AMORT"),
                Isp = D(k, "ISP"),
                Sad = D(k, "SAD"),
                Nab02 = D(k, "NAB02"),
                Isp02 = D(k, "ISP02"),
                Sad02 = D(k, "SAD02"),
                StopaOt2 = D(k, "STOPAOT2"),
                Amort2 = D(k, "AMORT2"),
                Isp2 = D(k, "ISP2"),
                Sad2 = D(k, "SAD2"),
            });
        }

        Poruka = $"Ukupno {Stavke.Count} zapisa.";
    }

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
                FontSize    = 8
            };
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run(Naslov))
                { FontSize = 13, FontWeight = System.Windows.FontWeights.Bold });

            var tbl = new System.Windows.Documents.Table { CellSpacing = 0 };
            foreach (var w in new double[] { 60, 170, 80, 65, 40, 90, 90, 90, 65, 90, 90, 90, 90, 90, 90, 65, 90, 90, 90, 65 })
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

            if (JeMrs)
                DodajRed(["Šifra", "Naziv", "Konto", "Mesto", "AG", "Nab0", "Isp0", "Sad0", "Stopa%", "Amort", "Isp", "Sad", "-", "-", "-", "-", "-", "-", "-", "-"], header: true);
            else if (JePoreskaStara)
                DodajRed(["Šifra", "Naziv", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "Nab02", "Isp02", "Sad02", "St.PP%", "-", "-", "-", "-"], header: true);
            else
                DodajRed(["Šifra", "Naziv", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "Amort2", "Isp2", "Sad2", "St.PP%"], header: true);

            foreach (var s in Stavke)
                DodajRed([s.Sifra, s.Naziv, s.Konto, s.Mesto, s.Ag,
                    s.Nab0.ToString("N2"), s.Isp0.ToString("N2"), s.Sad0.ToString("N2"),
                    s.StopaOt.ToString("N3"), s.Amort.ToString("N2"), s.Isp.ToString("N2"), s.Sad.ToString("N2"),
                    s.Nab02.ToString("N2"), s.Isp02.ToString("N2"), s.Sad02.ToString("N2"), s.StopaOt2.ToString("N3"),
                    s.Amort2.ToString("N2"), s.Isp2.ToString("N2"), s.Sad2.ToString("N2"), ""]);

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
            sw.WriteLine("Sifra;Naziv;Konto;Mesto;AG;Nab0;Isp0;Sad0;StopaOt;Amort;Isp;Sad;Nab02;Isp02;Sad02;StopaOt2;Amort2;Isp2;Sad2");
            foreach (var s in Stavke)
                sw.WriteLine($"{s.Sifra};{s.Naziv};{s.Konto};{s.Mesto};{s.Ag};{s.Nab0:N2};{s.Isp0:N2};{s.Sad0:N2};{s.StopaOt:N3};{s.Amort:N2};{s.Isp:N2};{s.Sad:N2};{s.Nab02:N2};{s.Isp02:N2};{s.Sad02:N2};{s.StopaOt2:N3};{s.Amort2:N2};{s.Isp2:N2};{s.Sad2:N2}");
            Poruka = $"CSV izvoz završen: {dlg.FileName} ({Stavke.Count} redova).";
        }
        catch (Exception ex) { Poruka = $"Greška izvoza: {ex.Message}"; }
    }

    private static decimal D(OsKartica k, string p)
        => OsSaldoViewModel.DajDec(k, p);
}
