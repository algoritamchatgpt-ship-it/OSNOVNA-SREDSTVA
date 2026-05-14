using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using System.Collections.ObjectModel;
using System.IO;

namespace OsnovnaSredstva.ViewModels;

public partial class OsSifarnikViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty] private int _aktivniTab = 0;
    [ObservableProperty] private string _poruka = "Kliknite na tab za učitavanje podataka.";

    [ObservableProperty] private ObservableCollection<OsVrstaStavka> _vrsteOs = [];
    [ObservableProperty] private ObservableCollection<OsAgStavka> _amortGrupe = [];
    [ObservableProperty] private ObservableCollection<OsAgPodStavka> _amortPodgrupe = [];
    [ObservableProperty] private ObservableCollection<LdGrupaStavka> _grupeOs = [];
    [ObservableProperty] private ObservableCollection<OsOsnKStavka> _osnKoriscenja = [];

    [ObservableProperty] private OsVrstaStavka? _izabranaVrsta;
    [ObservableProperty] private OsAgStavka? _izabranaAmortGrupa;
    [ObservableProperty] private OsAgPodStavka? _izabranaAmortPodgrupa;
    [ObservableProperty] private LdGrupaStavka? _izabranaGrupa;
    [ObservableProperty] private OsOsnKStavka? _izabraniOsnov;

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
        UcitajGrupeOs();
        UcitajOsnKoriscenja();
    }

    // ═══ UČITAVANJE ═══

    private void UcitajVrsteOs()
    {
        var path = DbfPutanja("osvrsta.dbf");
        if (path == null) { VrsteOs = []; return; }
        try
        {
            var reader = new SimpleDbfReader(path);
            VrsteOs = new ObservableCollection<OsVrstaStavka>(
                reader.Zapisi().Select(r => new OsVrstaStavka
                {
                    Vrsta   = r.DajString("VRSTA"),
                    Naziv   = r.DajString("NAZIV"),
                    Preneto = r.DajString("PRENETO"),
                    IDBr    = (int)r.DajDecimal("IDBR"),
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
            var reader = new SimpleDbfReader(path);
            AmortGrupe = new ObservableCollection<OsAgStavka>(
                reader.Zapisi().Select(r => new OsAgStavka
                {
                    Ag      = r.DajString("AG"),
                    AgStopa = r.DajDecimal("AGSTOPA"),
                    Opis    = r.DajString("OPIS"),
                    Vrsta   = r.DajString("VRSTA"),
                    Preneto = r.DajString("PRENETO"),
                    IDBr    = (int)r.DajDecimal("IDBR"),
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
            var reader = new SimpleDbfReader(path);
            AmortPodgrupe = new ObservableCollection<OsAgPodStavka>(
                reader.Zapisi().Select(r => new OsAgPodStavka
                {
                    AgPod   = r.DajString("AGPOD"),
                    Ag      = r.DajString("AG"),
                    Opis    = r.DajString("OPIS"),
                    Preneto = r.DajString("PRENETO"),
                    IDBr    = (int)r.DajDecimal("IDBR"),
                }));
        }
        catch (Exception ex) { AmortPodgrupe = []; Poruka = $"osagpod.dbf: {ex.Message}"; }
    }

    private void UcitajGrupeOs()
    {
        var path = DbfPutanja("ldgrupa.dbf");
        if (path == null) { GrupeOs = []; return; }
        try
        {
            var reader = new SimpleDbfReader(path);
            GrupeOs = new ObservableCollection<LdGrupaStavka>(
                reader.Zapisi().Select(r => new LdGrupaStavka
                {
                    Grupa   = (int)r.DajDecimal("GRUPA"),
                    Naziv   = r.DajString("NAZIV"),
                    Preneto = r.DajString("PRENETO"),
                    IDBr    = (int)r.DajDecimal("IDBR"),
                }));
        }
        catch (Exception ex) { GrupeOs = []; Poruka = $"ldgrupa.dbf: {ex.Message}"; }
    }

    private void UcitajOsnKoriscenja()
    {
        var path = DbfPutanja("ososnk.dbf");
        if (path == null) { OsnKoriscenja = []; return; }
        try
        {
            var reader = new SimpleDbfReader(path);
            OsnKoriscenja = new ObservableCollection<OsOsnKStavka>(
                reader.Zapisi().Select(r => new OsOsnKStavka
                {
                    OsnovKor = r.DajString("OSNOVKOR"),
                    Naziv    = r.DajString("NAZIV"),
                    Preneto  = r.DajString("PRENETO"),
                    IDBr     = (int)r.DajDecimal("IDBR"),
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
            case 0: VrsteOs.Add(new OsVrstaStavka()); break;
            case 1: AmortGrupe.Add(new OsAgStavka()); break;
            case 2: AmortPodgrupe.Add(new OsAgPodStavka()); break;
            case 3: GrupeOs.Add(new LdGrupaStavka()); break;
            case 4: OsnKoriscenja.Add(new OsOsnKStavka()); break;
        }
        Poruka = "Novi red dodan. Unesite podatke pa kliknite Sačuvaj.";
    }

    // ═══ OBRIŠI ═══

    [RelayCommand]
    private void Obrisi()
    {
        bool obrisan = false;
        switch (AktivniTab)
        {
            case 0:
                if (IzabranaVrsta != null) { VrsteOs.Remove(IzabranaVrsta); obrisan = true; }
                break;
            case 1:
                if (IzabranaAmortGrupa != null) { AmortGrupe.Remove(IzabranaAmortGrupa); obrisan = true; }
                break;
            case 2:
                if (IzabranaAmortPodgrupa != null) { AmortPodgrupe.Remove(IzabranaAmortPodgrupa); obrisan = true; }
                break;
            case 3:
                if (IzabranaGrupa != null) { GrupeOs.Remove(IzabranaGrupa); obrisan = true; }
                break;
            case 4:
                if (IzabraniOsnov != null) { OsnKoriscenja.Remove(IzabraniOsnov); obrisan = true; }
                break;
        }
        Poruka = obrisan ? "Red obrisan. Kliknite Sačuvaj da sačuvate promjenu." : "Nije izabran red za brisanje.";
    }

    // ═══ SAČUVAJ ═══

    [RelayCommand]
    private void Sacuvaj()
    {
        switch (AktivniTab)
        {
            case 0: SacuvajVrsteOs(); break;
            case 1: SacuvajAmortGrupe(); break;
            case 2: SacuvajAmortPodgrupe(); break;
            case 3: SacuvajGrupeOs(); break;
            case 4: SacuvajOsnKoriscenja(); break;
        }
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

    private void SacuvajGrupeOs()
    {
        var path = DbfPutanja("ldgrupa.dbf");
        if (path == null) { Poruka = "ldgrupa.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, GrupeOs.ToList(),
                (s, f) => f.ToUpperInvariant() switch
                {
                    "GRUPA"   => (object?)s.Grupa,
                    "NAZIV"   => s.Naziv,
                    "PRENETO" => s.Preneto,
                    "IDBR"    => (object?)s.IDBr,
                    _         => null
                });
            Poruka = $"Grupe OS sačuvane ({GrupeOs.Count} zapisa).";
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

    // ═══ NAVIGACIJA ═══

    [RelayCommand]
    private void PrikaziTab(string tabIndex)
    {
        if (int.TryParse(tabIndex, out var idx))
            AktivniTab = idx;
    }

    // ═══ OSVEŽI ═══

    [RelayCommand]
    private void Osvezi() => UcitajSve();

    // ═══ NAVIGACIJA REDOVA (OS0 stil) ═══

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
            3 => GrupeOs.Count,
            4 => OsnKoriscenja.Count,
            _ => 0
        };

    private int TrenutniIndexReda() =>
        AktivniTab switch
        {
            0 => IzabranaVrsta is null ? -1 : VrsteOs.IndexOf(IzabranaVrsta),
            1 => IzabranaAmortGrupa is null ? -1 : AmortGrupe.IndexOf(IzabranaAmortGrupa),
            2 => IzabranaAmortPodgrupa is null ? -1 : AmortPodgrupe.IndexOf(IzabranaAmortPodgrupa),
            3 => IzabranaGrupa is null ? -1 : GrupeOs.IndexOf(IzabranaGrupa),
            4 => IzabraniOsnov is null ? -1 : OsnKoriscenja.IndexOf(IzabraniOsnov),
            _ => -1
        };

    private void PostaviTekuciRed(int index)
    {
        var ukupno = TrenutniBrojRedova();
        if (ukupno == 0)
        {
            Poruka = "Nema redova za navigaciju.";
            return;
        }

        if (index < 0) index = 0;
        if (index >= ukupno) index = ukupno - 1;

        switch (AktivniTab)
        {
            case 0: IzabranaVrsta = VrsteOs[index]; break;
            case 1: IzabranaAmortGrupa = AmortGrupe[index]; break;
            case 2: IzabranaAmortPodgrupa = AmortPodgrupe[index]; break;
            case 3: IzabranaGrupa = GrupeOs[index]; break;
            case 4: IzabraniOsnov = OsnKoriscenja[index]; break;
        }

        Poruka = $"Pozicija reda: {index + 1}/{ukupno}.";
    }

    // ═══ HELPER ═══

    private string? DbfPutanja(string ime)
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder)) return null;

        foreach (var naziv in new[] { ime, ime.ToUpperInvariant() })
        {
            var p = Path.Combine(folder, naziv);
            if (File.Exists(p)) return p;
        }

        if (!Directory.Exists(folder)) return null;
        return Directory.GetFiles(folder, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Equals(ime, StringComparison.OrdinalIgnoreCase));
    }
}
