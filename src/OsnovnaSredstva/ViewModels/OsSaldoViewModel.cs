using CommunityToolkit.Mvvm.ComponentModel;
using OsnovnaSredstva.Models;
using System.Collections.ObjectModel;

namespace OsnovnaSredstva.ViewModels;

public partial class OsSaldoViewModel : ObservableObject
{
    public enum OsSaldoPrikazTip
    {
        Analitika,
        Sintetika,
        NabavkePoAgrupama,
        PocetnoStanje
    }

    [ObservableProperty] private string _naslov = "";
    [ObservableProperty] private string _poruka = "";

    public ObservableCollection<OsSaldoStavka> Stavke { get; } = [];
    public OsSaldoPrikazTip Prikaz { get; private set; } = OsSaldoPrikazTip.Analitika;
    public string NazivKljucneKolone { get; private set; } = "Sifra";

    public static OsSaldoViewModel PoKontu(IEnumerable<OsKartica> kartice)
    {
        var vm = new OsSaldoViewModel();
        vm.Prikaz = OsSaldoPrikazTip.Analitika;
        vm.NazivKljucneKolone = "Konto";
        vm.Naslov = "SALDO ANALITIKA";
        vm.Ucitaj(kartice, k => string.IsNullOrWhiteSpace(k.Konto) ? "(bez konta)" : k.Konto.Trim());
        return vm;
    }

    public static OsSaldoViewModel PoKontuSintetika(IEnumerable<OsKartica> kartice)
    {
        var vm = new OsSaldoViewModel();
        vm.Prikaz = OsSaldoPrikazTip.Sintetika;
        vm.NazivKljucneKolone = "Konto";
        vm.Naslov = "SALDO SINTETIKA";
        vm.Ucitaj(
            kartice,
            k => string.IsNullOrWhiteSpace(k.Konto) ? "(bez konta)" : k.Konto.Trim(),
            ukljuciTekuciMrs: true,
            ukljuciPoreskaPolja: false);
        return vm;
    }

    public static OsSaldoViewModel SaldoNabavkePoAgrupama(IEnumerable<OsKartica> kartice, DateTime? periodOd)
    {
        var vm = new OsSaldoViewModel();
        vm.Prikaz = OsSaldoPrikazTip.NabavkePoAgrupama;
        vm.NazivKljucneKolone = "Konto";
        vm.Naslov = "SALDO NABAVKE PO A.GRUPAMA";

        var filtrirane = periodOd.HasValue
            ? kartice.Where(k => k.DatNab.HasValue && k.DatNab.Value.Date < periodOd.Value.Date)
            : kartice;

        vm.Ucitaj(
            filtrirane,
            k => string.IsNullOrWhiteSpace(k.Konto) ? "(bez konta)" : k.Konto.Trim(),
            ukljuciTekuciMrs: false,
            ukljuciPoreskaPolja: false);

        if (periodOd.HasValue)
            vm.Poruka += $"  (DATNAB < {periodOd.Value:dd.MM.yyyy})";

        return vm;
    }

    public static OsSaldoViewModel PocetnoStanjePoAg(IEnumerable<OsKartica> kartice, DateTime? periodOd)
    {
        var vm = new OsSaldoViewModel();
        vm.Prikaz = OsSaldoPrikazTip.PocetnoStanje;
        vm.NazivKljucneKolone = "AG";
        vm.Naslov = "POCETNO STANJE";

        var filtrirane = periodOd.HasValue
            ? kartice.Where(k => k.DatNab.HasValue && k.DatNab.Value.Date >= periodOd.Value.Date)
            : kartice;

        vm.Ucitaj(
            filtrirane,
            k => string.IsNullOrWhiteSpace(k.Ag) ? "(bez AG)" : k.Ag.Trim(),
            ukljuciTekuciMrs: true,
            ukljuciPoreskaPolja: true);

        if (periodOd.HasValue)
            vm.Poruka += $"  (DATNAB >= {periodOd.Value:dd.MM.yyyy})";

        return vm;
    }

    public static OsSaldoViewModel PoMestu(IEnumerable<OsKartica> kartice, string? kontoFilter = null)
    {
        var vm = new OsSaldoViewModel();
        vm.Prikaz = OsSaldoPrikazTip.Analitika;
        vm.NazivKljucneKolone = "Mesto";
        var konto = (kontoFilter ?? string.Empty).Trim();

        vm.Naslov = string.IsNullOrWhiteSpace(konto)
            ? "SALDO PO MESTU TROSKOVA"
            : $"SALDO PO MESTU TROSKOVA - KONTO {konto}";

        var filtrirane = string.IsNullOrWhiteSpace(konto)
            ? kartice
            : kartice.Where(k => string.Equals((k.Konto ?? string.Empty).Trim(), konto, StringComparison.OrdinalIgnoreCase));

        vm.Ucitaj(filtrirane, k => string.IsNullOrWhiteSpace(k.Mesto) ? "(bez mesta)" : k.Mesto.Trim());
        return vm;
    }

    public static OsSaldoViewModel PoPopisu(IEnumerable<OsKartica> kartice, string naslov)
    {
        var vm = new OsSaldoViewModel();
        vm.Prikaz = OsSaldoPrikazTip.Analitika;
        vm.NazivKljucneKolone = "Konto";
        vm.Naslov = naslov;
        vm.Ucitaj(kartice, k => string.IsNullOrWhiteSpace(k.Konto) ? "(bez konta)" : k.Konto.Trim());
        return vm;
    }

    private void Ucitaj(
        IEnumerable<OsKartica> kartice,
        Func<OsKartica, string> kljuc,
        bool ukljuciTekuciMrs = true,
        bool ukljuciPoreskaPolja = true)
    {
        var svi = kartice.ToList();
        var grupe = new SortedDictionary<string, OsSaldoStavka>(StringComparer.OrdinalIgnoreCase);

        foreach (var k in svi)
        {
            var sifra = kljuc(k);
            if (!grupe.TryGetValue(sifra, out var s))
            {
                s = new OsSaldoStavka { Sifra = sifra };
                grupe[sifra] = s;
            }

            s.BrojKartica++;
            s.Nab0 += k.Nab0;
            s.Isp0 += k.Isp0;
            s.Sad0 += k.Sad0;

            if (ukljuciTekuciMrs)
            {
                s.Nab += DajDec(k, "NAB");
                s.Isp += DajDec(k, "ISP");
                s.Sad += DajDec(k, "SAD");
                s.Amort += DajDec(k, "AMORT");
            }

            if (ukljuciPoreskaPolja)
            {
                s.Nab02 += DajDec(k, "NAB02");
                s.Isp02 += DajDec(k, "ISP02");
                s.Sad02 += DajDec(k, "SAD02");
                s.Nab2 += DajDec(k, "NAB2");
                s.Isp2 += DajDec(k, "ISP2");
                s.Amort2 += DajDec(k, "AMORT2");
                s.Sad2 += DajDec(k, "SAD2");
            }
        }

        foreach (var s in grupe.Values) Stavke.Add(s);
        Poruka = $"Ukupno {grupe.Count} grupe, {svi.Count} kartica.";
    }

    internal static decimal DajDec(OsKartica k, string polje)
    {
        if (!k.ExtraPolja.TryGetValue(polje, out var val) || val is null) return 0m;
        return val switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double db => (decimal)db,
            _ when decimal.TryParse(val.ToString(),
                       System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out var d) => d,
            _ => 0m
        };
    }
}
