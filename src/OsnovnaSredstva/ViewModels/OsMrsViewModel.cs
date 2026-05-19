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

    private static decimal D(OsKartica k, string p)
        => OsSaldoViewModel.DajDec(k, p);
}
