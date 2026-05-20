using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class OsSifarnikViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty] private int    _aktivniTab = 0;
    [ObservableProperty] private string _poruka     = "";

    private bool _izmijenjeno;
    public bool ImaNeSnimljenih => _izmijenjeno;

    [ObservableProperty] private ObservableCollection<OsVrstaStavka>  _vrsteOs           = [];
    [ObservableProperty] private ObservableCollection<OsAgStavka>     _amortGrupe        = [];
    [ObservableProperty] private ObservableCollection<OsAgPodStavka>  _amortPodgrupe     = [];
    [ObservableProperty] private ObservableCollection<OsIzvorStavka>  _izvoriFinansiranja = [];
    [ObservableProperty] private ObservableCollection<OsOsnKStavka>   _osnKoriscenja     = [];

    [ObservableProperty] private OsVrstaStavka?  _izabranaVrsta;
    [ObservableProperty] private OsAgStavka?     _izabranaAmortGrupa;
    [ObservableProperty] private OsAgPodStavka?  _izabranaAmortPodgrupa;
    [ObservableProperty] private OsIzvorStavka?  _izabraniIzvor;
    [ObservableProperty] private OsOsnKStavka?   _izabraniOsnov;

    public OsSifarnikViewModel(AppState appState)
    {
        _appState = appState;
        UcitajSve();
    }

    private void UcitajSve()
    {
        UcitajVrsteOs();
        UcitajAmortGrupe();
        UcitajAmortPodgrupe();
        UcitajIzvorFinansiranja();
        UcitajOsnKoriscenja();
        Poruka = $"Učitano: Vrste OS ({VrsteOs.Count}), " +
                 $"Amort. grupe ({AmortGrupe.Count}), " +
                 $"Podgrupe ({AmortPodgrupe.Count}), " +
                 $"Izvor fin. ({IzvoriFinansiranja.Count}), " +
                 $"Osnov korišćenja ({OsnKoriscenja.Count})";
        _izmijenjeno = false;
    }

    // ═══ UČITAVANJE ═══

    private void UcitajVrsteOs()
    {
        var path = DbfPutanja("osvrsta.dbf");
        if (path == null) { VrsteOs = []; return; }
        try
        {
            VrsteOs = new ObservableCollection<OsVrstaStavka>(
                DbfReader.CitajSveZapise(path).Select(r => new OsVrstaStavka
                {
                    Vrsta   = DbfReader.Str(r, "VRSTA"),
                    Naziv   = DbfReader.Str(r, "NAZIV"),
                    Preneto = DbfReader.Str(r, "PRENETO"),
                    IDBr    = (int)DbfReader.Dec(r, "IDBR"),
                }));
        }
        catch (Exception ex) { VrsteOs = []; Poruka = $"osvrsta.dbf: {ex.Message}"; }
    }

    private void UcitajAmortGrupe()
    {
        var path = DbfPutanja("osag.dbf");
        if (path == null) { AmortGrupe = []; return; }
        try
        {
            AmortGrupe = new ObservableCollection<OsAgStavka>(
                DbfReader.CitajSveZapise(path).Select(r => new OsAgStavka
                {
                    Ag      = DbfReader.Str(r, "AG"),
                    AgStopa = DbfReader.Dec(r, "AGSTOPA"),
                    Opis    = DbfReader.Str(r, "OPIS"),
                    Vrsta   = DbfReader.Str(r, "VRSTA"),
                    Preneto = DbfReader.Str(r, "PRENETO"),
                    IDBr    = (int)DbfReader.Dec(r, "IDBR"),
                }));
        }
        catch (Exception ex) { AmortGrupe = []; Poruka = $"osag.dbf: {ex.Message}"; }
    }

    private void UcitajAmortPodgrupe()
    {
        var path = DbfPutanja("osagpod.dbf");
        if (path == null) { AmortPodgrupe = []; return; }
        try
        {
            AmortPodgrupe = new ObservableCollection<OsAgPodStavka>(
                DbfReader.CitajSveZapise(path).Select(r => new OsAgPodStavka
                {
                    AgPod   = DbfReader.Str(r, "AGPOD"),
                    Ag      = DbfReader.Str(r, "AG"),
                    Opis    = DbfReader.Str(r, "OPIS"),
                    Preneto = DbfReader.Str(r, "PRENETO"),
                    IDBr    = (int)DbfReader.Dec(r, "IDBR"),
                }));
        }
        catch (Exception ex) { AmortPodgrupe = []; Poruka = $"osagpod.dbf: {ex.Message}"; }
    }

    private void UcitajIzvorFinansiranja()
    {
        var path = DbfPutanja("osizvorf.dbf");
        if (path == null) { IzvoriFinansiranja = []; return; }
        try
        {
            IzvoriFinansiranja = new ObservableCollection<OsIzvorStavka>(
                DbfReader.CitajSveZapise(path).Select(r => new OsIzvorStavka
                {
                    Izvor   = DbfReader.Str(r, "IZVOR"),
                    Naziv   = DbfReader.Str(r, "NAZIV"),
                    Preneto = DbfReader.Str(r, "PRENETO"),
                    IDBr    = (int)DbfReader.Dec(r, "IDBR"),
                }));
        }
        catch (Exception ex) { IzvoriFinansiranja = []; Poruka = $"osizvorf.dbf: {ex.Message}"; }
    }

    private void UcitajOsnKoriscenja()
    {
        var path = DbfPutanja("ososnk.dbf");
        if (path == null) { OsnKoriscenja = []; return; }
        try
        {
            OsnKoriscenja = new ObservableCollection<OsOsnKStavka>(
                DbfReader.CitajSveZapise(path).Select(r => new OsOsnKStavka
                {
                    OsnovKor = DbfReader.Str(r, "OSNOVKOR"),
                    Naziv    = DbfReader.Str(r, "NAZIV"),
                    Preneto  = DbfReader.Str(r, "PRENETO"),
                    IDBr     = (int)DbfReader.Dec(r, "IDBR"),
                }));
        }
        catch (Exception ex) { OsnKoriscenja = []; Poruka = $"ososnk.dbf: {ex.Message}"; }
    }

    // ═══ DODAJ ═══

    [RelayCommand]
    private void Dodaj()
    {
        switch (AktivniTab)
        {
            case 0:
                var v = new OsVrstaStavka { IDBr = SledeciIdbr(VrsteOs.Select(x => x.IDBr)), Preneto = "N" };
                VrsteOs.Add(v); IzabranaVrsta = v; break;
            case 1:
                var g = new OsAgStavka { IDBr = SledeciIdbr(AmortGrupe.Select(x => x.IDBr)), Preneto = "N" };
                AmortGrupe.Add(g); IzabranaAmortGrupa = g; break;
            case 2:
                var p = new OsAgPodStavka { IDBr = SledeciIdbr(AmortPodgrupe.Select(x => x.IDBr)), Preneto = "N" };
                AmortPodgrupe.Add(p); IzabranaAmortPodgrupa = p; break;
            case 3:
                var i = new OsIzvorStavka { IDBr = SledeciIdbr(IzvoriFinansiranja.Select(x => x.IDBr)), Preneto = "N" };
                IzvoriFinansiranja.Add(i); IzabraniIzvor = i; break;
            case 4:
                var o = new OsOsnKStavka { IDBr = SledeciIdbr(OsnKoriscenja.Select(x => x.IDBr)), Preneto = "N" };
                OsnKoriscenja.Add(o); IzabraniOsnov = o; break;
        }
        Poruka = "Novi red dodan. Unesite podatke i kliknite Sačuvaj.";
        _izmijenjeno = true;
    }

    private static int SledeciIdbr(IEnumerable<int> existingIds)
    {
        var ids = existingIds.ToList();
        return ids.Count == 0 ? 1 : ids.Max() + 1;
    }

    private void OtvoriKarticuZaTrenutni()
    {
        OsSifarnikKarticaViewModel? vm = AktivniTab switch
        {
            0 when IzabranaVrsta         != null => OsSifarnikKarticaViewModel.ZaVrstu(IzabranaVrsta),
            1 when IzabranaAmortGrupa    != null => OsSifarnikKarticaViewModel.ZaAg(IzabranaAmortGrupa),
            2 when IzabranaAmortPodgrupa != null => OsSifarnikKarticaViewModel.ZaAgPod(IzabranaAmortPodgrupa),
            3 when IzabraniIzvor         != null => OsSifarnikKarticaViewModel.ZaIzvor(IzabraniIzvor),
            4 when IzabraniOsnov         != null => OsSifarnikKarticaViewModel.ZaOsnov(IzabraniOsnov),
            _ => null
        };
        if (vm == null) return;

        var win = new OsSifarnikKarticaWindow(vm);
        if (win.ShowDialog() == true)
            Poruka = $"Kartica sačuvana. Kliknite SAČUVAJ da zapišete u DBF.";
        else
        {
            // Korisnik otkazio — ukloni samo-dodati prazni red
            switch (AktivniTab)
            {
                case 0: if (IzabranaVrsta         != null) VrsteOs.Remove(IzabranaVrsta);                 break;
                case 1: if (IzabranaAmortGrupa    != null) AmortGrupe.Remove(IzabranaAmortGrupa);         break;
                case 2: if (IzabranaAmortPodgrupa != null) AmortPodgrupe.Remove(IzabranaAmortPodgrupa);   break;
                case 3: if (IzabraniIzvor         != null) IzvoriFinansiranja.Remove(IzabraniIzvor);      break;
                case 4: if (IzabraniOsnov         != null) OsnKoriscenja.Remove(IzabraniOsnov);           break;
            }
            Poruka = "Dodavanje otkazano.";
        }
    }

    // ═══ OBRIŠI ═══

    [RelayCommand]
    private void Obrisi()
    {
        var opis = AktivniTab switch
        {
            0 when IzabranaVrsta         != null => $"vrstu '{IzabranaVrsta.Vrsta}'",
            1 when IzabranaAmortGrupa    != null => $"grupu '{IzabranaAmortGrupa.Ag}'",
            2 when IzabranaAmortPodgrupa != null => $"podgrupu '{IzabranaAmortPodgrupa.AgPod}'",
            3 when IzabraniIzvor         != null => $"izvor '{IzabraniIzvor.Izvor}'",
            4 when IzabraniOsnov         != null => $"osnov '{IzabraniOsnov.OsnovKor}'",
            _ => null
        };

        if (opis == null) { Poruka = "Nije izabran red za brisanje."; return; }

        if (MessageBox.Show($"Brisanje: {opis}\n\nDa li ste sigurni?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        switch (AktivniTab)
        {
            case 0: VrsteOs.Remove(IzabranaVrsta!);                 break;
            case 1: AmortGrupe.Remove(IzabranaAmortGrupa!);         break;
            case 2: AmortPodgrupe.Remove(IzabranaAmortPodgrupa!);   break;
            case 3: IzvoriFinansiranja.Remove(IzabraniIzvor!);      break;
            case 4: OsnKoriscenja.Remove(IzabraniOsnov!);           break;
        }
        Poruka = $"Obrisan {opis}. Kliknite SAČUVAJ da sačuvate promenu.";
        _izmijenjeno = true;
    }

    // ═══ SAČUVAJ ═══

    [RelayCommand]
    private void Sacuvaj()
    {
        switch (AktivniTab)
        {
            case 0: SacuvajVrsteOs();           break;
            case 1: SacuvajAmortGrupe();        break;
            case 2: SacuvajAmortPodgrupe();     break;
            case 3: SacuvajIzvorFinansiranja(); break;
            case 4: SacuvajOsnKoriscenja();     break;
        }
        _izmijenjeno = false;
    }

    private void SacuvajVrsteOs()
    {
        var path = DbfPutanja("osvrsta.dbf");
        if (path == null) { Poruka = "osvrsta.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, VrsteOs.ToList(),
                (s, f) => f.ToUpperInvariant() switch
                {
                    "VRSTA"   => (object?)s.Vrsta,
                    "NAZIV"   => s.Naziv,
                    "PRENETO" => s.Preneto,
                    "IDBR"    => (object?)s.IDBr,
                    _         => null
                });
            Poruka = $"Vrste OS sačuvane ({VrsteOs.Count} zapisa).";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    private void SacuvajAmortGrupe()
    {
        var path = DbfPutanja("osag.dbf");
        if (path == null) { Poruka = "osag.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, AmortGrupe.ToList(),
                (s, f) => f.ToUpperInvariant() switch
                {
                    "AG"      => (object?)s.Ag,
                    "AGSTOPA" => s.AgStopa,
                    "OPIS"    => s.Opis,
                    "VRSTA"   => s.Vrsta,
                    "PRENETO" => s.Preneto,
                    "IDBR"    => (object?)s.IDBr,
                    _         => null
                });
            Poruka = $"Amortizacione grupe sačuvane ({AmortGrupe.Count} zapisa).";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    private void SacuvajAmortPodgrupe()
    {
        var path = DbfPutanja("osagpod.dbf");
        if (path == null) { Poruka = "osagpod.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, AmortPodgrupe.ToList(),
                (s, f) => f.ToUpperInvariant() switch
                {
                    "AGPOD"   => (object?)s.AgPod,
                    "AG"      => s.Ag,
                    "OPIS"    => s.Opis,
                    "PRENETO" => s.Preneto,
                    "IDBR"    => (object?)s.IDBr,
                    _         => null
                });
            Poruka = $"Podgrupe amortizacije sačuvane ({AmortPodgrupe.Count} zapisa).";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    private void SacuvajIzvorFinansiranja()
    {
        var path = DbfPutanja("osizvorf.dbf");
        if (path == null) { Poruka = "osizvorf.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, IzvoriFinansiranja.ToList(),
                (s, f) => f.ToUpperInvariant() switch
                {
                    "IZVOR"   => (object?)s.Izvor,
                    "NAZIV"   => s.Naziv,
                    "PRENETO" => s.Preneto,
                    "IDBR"    => (object?)s.IDBr,
                    _         => null
                });
            Poruka = $"Izvor finansiranja sačuvan ({IzvoriFinansiranja.Count} zapisa).";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    private void SacuvajOsnKoriscenja()
    {
        var path = DbfPutanja("ososnk.dbf");
        if (path == null) { Poruka = "ososnk.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, OsnKoriscenja.ToList(),
                (s, f) => f.ToUpperInvariant() switch
                {
                    "OSNOVKOR" => (object?)s.OsnovKor,
                    "NAZIV"    => s.Naziv,
                    "PRENETO"  => s.Preneto,
                    "IDBR"     => (object?)s.IDBr,
                    _          => null
                });
            Poruka = $"Osnov korišćenja sačuvan ({OsnKoriscenja.Count} zapisa).";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    // ═══ KARTICA — otvara dijalog za uređivanje izabranog reda ═══

    [RelayCommand]
    private void Kartica()
    {
        var imaIzbor = AktivniTab switch
        {
            0 => IzabranaVrsta         != null,
            1 => IzabranaAmortGrupa    != null,
            2 => IzabranaAmortPodgrupa != null,
            3 => IzabraniIzvor         != null,
            4 => IzabraniOsnov         != null,
            _ => false
        };

        if (!imaIzbor)
        {
            Poruka = "Nije izabran red. Kliknite na red u tabeli pa zatim KARTICA.";
            return;
        }

        OtvoriKarticuZaTrenutni();
    }

    // ═══ POPISNA LISTA ═══

    [RelayCommand]
    private void PopisnaLista()
    {
        var tip = AktivniTab switch
        {
            0 => OsSifarnikPopisnaListaViewModel.Tip.VrsteOs,
            1 => OsSifarnikPopisnaListaViewModel.Tip.AmortGrupe,
            2 => OsSifarnikPopisnaListaViewModel.Tip.AmortPodgrupe,
            3 => OsSifarnikPopisnaListaViewModel.Tip.IzvoriFinansiranja,
            4 => OsSifarnikPopisnaListaViewModel.Tip.OsnKoriscenja,
            _ => OsSifarnikPopisnaListaViewModel.Tip.VrsteOs
        };

        var vm = new OsSifarnikPopisnaListaViewModel(
            tip,
            VrsteOs, AmortGrupe, AmortPodgrupe, IzvoriFinansiranja, OsnKoriscenja);

        var win = new OsSifarnikPopisnaListaWindow(vm);
        win.ShowDialog();
    }

    // ═══ NAVIGACIJA ═══

    [RelayCommand]
    private void PrikaziTab(string tabIndex)
    {
        if (int.TryParse(tabIndex, out var idx))
            AktivniTab = idx;
    }

    [RelayCommand]
    private void Osvezi() => UcitajSve();

    [RelayCommand]
    private void Prvi() => PostaviTekuciRed(0);

    [RelayCommand]
    private void Zadnji()
    {
        var poslednji = TrenutniBrojRedova() - 1;
        if (poslednji >= 0) PostaviTekuciRed(poslednji);
    }

    [RelayCommand]
    private void Dole() => PostaviTekuciRed(TrenutniIndexReda() + 1);

    [RelayCommand]
    private void Gore() => PostaviTekuciRed(TrenutniIndexReda() - 1);

    private int TrenutniBrojRedova() =>
        AktivniTab switch
        {
            0 => VrsteOs.Count,
            1 => AmortGrupe.Count,
            2 => AmortPodgrupe.Count,
            3 => IzvoriFinansiranja.Count,
            4 => OsnKoriscenja.Count,
            _ => 0
        };

    private int TrenutniIndexReda() =>
        AktivniTab switch
        {
            0 => IzabranaVrsta         is null ? -1 : VrsteOs.IndexOf(IzabranaVrsta),
            1 => IzabranaAmortGrupa    is null ? -1 : AmortGrupe.IndexOf(IzabranaAmortGrupa),
            2 => IzabranaAmortPodgrupa is null ? -1 : AmortPodgrupe.IndexOf(IzabranaAmortPodgrupa),
            3 => IzabraniIzvor         is null ? -1 : IzvoriFinansiranja.IndexOf(IzabraniIzvor),
            4 => IzabraniOsnov         is null ? -1 : OsnKoriscenja.IndexOf(IzabraniOsnov),
            _ => -1
        };

    private void PostaviTekuciRed(int index)
    {
        var ukupno = TrenutniBrojRedova();
        if (ukupno == 0) { Poruka = "Nema redova za navigaciju."; return; }

        index = Math.Clamp(index, 0, ukupno - 1);

        switch (AktivniTab)
        {
            case 0: IzabranaVrsta         = VrsteOs[index];           break;
            case 1: IzabranaAmortGrupa    = AmortGrupe[index];        break;
            case 2: IzabranaAmortPodgrupa = AmortPodgrupe[index];     break;
            case 3: IzabraniIzvor         = IzvoriFinansiranja[index]; break;
            case 4: IzabraniOsnov         = OsnKoriscenja[index];     break;
        }

        Poruka = $"Pozicija: {index + 1}/{ukupno}.";
    }

    // ═══ HELPER ═══

    private string? DbfPutanja(string ime)
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder)) return null;

        // 1. Firma folder
        var hit = NadjiDbf(folder, ime);
        if (hit != null) return hit;

        // 2. FIN_ROOT/data00 (globalne tablice: osag.dbf, osagpod.dbf, ...)
        var root = FinWorkspaceResolver.NormalizeRootPath(folder);
        hit = NadjiDbf(Path.Combine(root, "data00"), ime);
        if (hit != null) return hit;

        // 3. Fallback: data00 pored exe (development/installed)
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
