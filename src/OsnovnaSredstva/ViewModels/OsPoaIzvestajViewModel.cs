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
