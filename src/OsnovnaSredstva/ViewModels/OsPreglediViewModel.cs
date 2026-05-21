using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class OsPreglediViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty] private string _poruka = "Izaberite pregled.";

    public OsPreglediViewModel(AppState appState) => _appState = appState;

    private static readonly HashSet<string> _poznataPolja = new(StringComparer.OrdinalIgnoreCase)
    {
        "OSIFRA","NAZ","DATNAB","BRNAL","KONTO","VRSTA",
        "AG","AGPOD","INVBROJ","MESTO","NAB0","ISP0","SAD0",
        "KOM","CENA","STOPAOT","OSNOVKOR","IZVOR","PRENETO","IDBR"
    };

    private List<OsKartica>? UcitajKartice()
    {
        var path = DbfHelper.NadjiDbf(_appState, "os.dbf");
        if (path == null)
        {
            Poruka = "os.dbf nije pronađen u folderu firme.";
            return null;
        }
        try
        {
            var reader = new SimpleDbfReader(path);
            var list = new List<OsKartica>();
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
                    if (!_poznataPolja.Contains(field.Name))
                    {
                        k.ExtraPolja[field.Name] = field.Type switch
                        {
                            'D'          => (object?)r.DajDate(field.Name),
                            'N' or 'F'   => r.DajDecimal(field.Name),
                            'L'          => r.DajBool(field.Name),
                            _            => r.DajString(field.Name)
                        };
                    }
                }
                list.Add(k);
            }
            Poruka = $"Učitano {list.Count} zapisa iz os.dbf.";
            return list;
        }
        catch (Exception ex)
        {
            Poruka = $"Greška učitavanja: {ex.Message}";
            return null;
        }
    }

    private DateTime? PeriodOd() => new DateTime(_appState.AktivnaGodina, 1, 1);

    // --- MRS pregledi ---

    [RelayCommand]
    private void OtMrs()
    {
        var k = UcitajKartice(); if (k == null) return;
        var vm = OsMrsViewModel.MrsPregled(k, skraceni: false);
        new OsMrsWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    [RelayCommand]
    private void OtMrsSkraceni()
    {
        var k = UcitajKartice(); if (k == null) return;
        var vm = OsMrsViewModel.MrsPregled(k, skraceni: true);
        new OsMrsWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    [RelayCommand]
    private void OtPoreskaStara()
    {
        var k = UcitajKartice(); if (k == null) return;
        var vm = OsMrsViewModel.PoreskaStara(k);
        new OsMrsWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    [RelayCommand]
    private void OtPoreskaNova()
    {
        var k = UcitajKartice(); if (k == null) return;
        var vm = OsMrsViewModel.PoreskaNova(k);
        new OsMrsWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    // --- Saldo pregledi ---

    [RelayCommand]
    private void OtSaldoAnalitika()
    {
        var k = UcitajKartice(); if (k == null) return;
        var vm = OsSaldoViewModel.PoKontu(k);
        new OsSaldoWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    [RelayCommand]
    private void OtSaldoSintetika()
    {
        var k = UcitajKartice(); if (k == null) return;
        var vm = OsSaldoViewModel.PoKontuSintetika(k);
        new OsSaldoWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    [RelayCommand]
    private void OtSaldoPoMestu()
    {
        var k = UcitajKartice(); if (k == null) return;
        var konto = Microsoft.VisualBasic.Interaction.InputBox(
            "Unesite konto za filter (ostavite prazno za sve):", "Saldo po mestu troškova", "");
        var vm = OsSaldoViewModel.PoMestu(k, string.IsNullOrWhiteSpace(konto) ? null : konto);
        new OsSaldoWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    [RelayCommand]
    private void OtSaldoNabavkePoAg()
    {
        var k = UcitajKartice(); if (k == null) return;
        var vm = OsSaldoViewModel.SaldoNabavkePoAgrupama(k, PeriodOd());
        new OsSaldoWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    [RelayCommand]
    private void OtPocetnoStanje()
    {
        var k = UcitajKartice(); if (k == null) return;
        var vm = OsSaldoViewModel.PocetnoStanjePoAg(k, PeriodOd());
        new OsSaldoWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    // --- POA izveštaji ---

    [RelayCommand]
    private void OtPoaObrazac()
    {
        var k = UcitajKartice(); if (k == null) return;
        var vm = new OsPoaIzvestajViewModel(k, OsPoaIzvestajViewModel.TipIzvestaja.PoaObrazac, null);
        new OsPoaIzvestajWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    [RelayCommand]
    private void OtEvidencijaPoa()
    {
        var k = UcitajKartice(); if (k == null) return;
        var vm = new OsPoaIzvestajViewModel(k, OsPoaIzvestajViewModel.TipIzvestaja.EvidencijaPoa, null);
        new OsPoaIzvestajWindow(vm) { Owner = Application.Current.MainWindow }.ShowDialog();
    }
}
