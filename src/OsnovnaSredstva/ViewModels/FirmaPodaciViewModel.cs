using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using System.IO;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class FirmaPodaciViewModel : ObservableObject
{
    private readonly AppState _appState;
    private Firma _original = new();
    private DbfOptimisticConcurrency.FileSnapshot? _firmaSnapshot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Naslov))]
    [NotifyCanExecuteChangedFor(nameof(SacuvajCommand))]
    [NotifyCanExecuteChangedFor(nameof(OtkaziCommand))]
    private bool _jeIzmena;

    [ObservableProperty]
    private Firma _firma = new();

    [ObservableProperty]
    private string _poruka = "";

    public FirmaPodaciViewModel(AppState appState)
    {
        _appState = appState;
        Ucitaj();
    }

    public string Naslov => string.IsNullOrWhiteSpace(Firma.Naziv)
        ? "PODACI O FIRMI"
        : $"PODACI O FIRMI - {Firma.Naziv}";

    [RelayCommand]
    private void Izmeni()
    {
        _original = Kopiraj(Firma);
        JeIzmena = true;
        Poruka = "Rezim izmene je aktivan. Unesite promene pa kliknite Sačuvaj.";
    }

    [RelayCommand(CanExecute = nameof(MozeSacuvaj))]
    private Task Sacuvaj()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return Task.CompletedTask;
        }

        try
        {
            var dbfPath = PronadjiDbf(folderPath, "firma.dbf")
                ?? FinWorkspaceResolver.EnsureFirmaDbf(folderPath);

            if (_firmaSnapshot != null && DbfOptimisticConcurrency.HasFileChanged(dbfPath, _firmaSnapshot))
            {
                var r = MessageBox.Show(
                    "Fajl firma.dbf je izmenjen od strane drugog korisnika.\nNastaviti sa čuvanjem (prepisati)?",
                    "Upozorenje — dual korisnici",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return Task.CompletedTask;
            }

            var schema = DbfTableWriter.LoadSchema(dbfPath);
            var red = TryUcitajPostojeciRed(dbfPath)
                ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            AzurirajFirmaRed(red, Firma);

            DbfTableWriter.WriteTable(
                dbfPath,
                schema,
                [red],
                static (r, fieldName) => r.TryGetValue(fieldName, out var v) ? v : null);

            _appState.AktivnaFirma?.Let(f =>
            {
                f.Naziv              = Firma.Naziv;
                f.Naziv2             = Firma.Naziv2;
                f.Baza               = Firma.Baza;
                f.PdvObveznik        = Firma.PdvObveznik;
                f.SifraDelatnosti    = Firma.SifraDelatnosti;
                f.NazivDelatnosti    = Firma.NazivDelatnosti;
                f.Vlasnik            = Firma.Vlasnik;
                f.OdgovornoLice      = Firma.OdgovornoLice;
                f.OrganizacioniOblik = Firma.OrganizacioniOblik;
                f.Maticni            = Firma.Maticni;
                f.Pib                = Firma.Pib;
                f.PostanskiBroj      = Firma.PostanskiBroj;
                f.Mesto              = Firma.Mesto;
                f.Ulica              = Firma.Ulica;
                f.BrojUlice          = Firma.BrojUlice;
                f.Opstina            = Firma.Opstina;
                f.Republika          = Firma.Republika;
                f.Drzava             = Firma.Drzava;
                f.Telefon1           = Firma.Telefon1;
                f.Telefon2           = Firma.Telefon2;
                f.Fax1               = Firma.Fax1;
                f.Email              = Firma.Email;
                f.Web                = Firma.Web;
                f.Agencija           = Firma.Agencija;
                f.ZiroRacun          = Firma.ZiroRacun;
                f.ZiroRacun2         = Firma.ZiroRacun2;
                f.ZiroRacunDevizni   = Firma.ZiroRacunDevizni;
                f.ZiroRacunBolovanje = Firma.ZiroRacunBolovanje;
                f.Banka1             = Firma.Banka1;
                f.Banka2             = Firma.Banka2;
                f.BankaDevizna       = Firma.BankaDevizna;
                f.BankaBolovanje     = Firma.BankaBolovanje;
                f.SwiftKod           = Firma.SwiftKod;
                f.DatumOsnivanja     = Firma.DatumOsnivanja;
                f.DatumRegistracije  = Firma.DatumRegistracije;
                f.DatumUpisa         = Firma.DatumUpisa;
                f.DatumPdv           = Firma.DatumPdv;
                f.RegBrojSocijalno   = Firma.RegBrojSocijalno;
                f.RegBrojZdravstveno = Firma.RegBrojZdravstveno;
                f.SudskiRegistar     = Firma.SudskiRegistar;
            });

            _firmaSnapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(dbfPath);
            _original = Kopiraj(Firma);
            JeIzmena = false;
            Poruka = "Podaci o firmi su sačuvani u firma.dbf.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čuvanju: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(MozeSacuvaj))]
    private void Otkazi()
    {
        Firma = Kopiraj(_original);
        JeIzmena = false;
        Poruka = "Izmene su otkazane.";
    }

    private bool MozeSacuvaj() => JeIzmena;

    private void Ucitaj()
    {
        var aktivna = _appState.AktivnaFirma;
        if (aktivna is null)
        {
            Firma = new Firma();
            _original = Kopiraj(Firma);
            Poruka = "Aktivna firma nije izabrana.";
            return;
        }

        var folderPath = aktivna.FolderPath;
        var dbfPath = PronadjiDbf(folderPath, "firma.dbf");
        if (dbfPath is null)
        {
            Firma = Kopiraj(aktivna);
            _original = Kopiraj(aktivna);
            Poruka = "firma.dbf nije pronađen — učitani su osnovni podaci.";
            return;
        }

        try
        {
            var red = TryUcitajPostojeciRed(dbfPath);
            if (red is null)
            {
                Firma = Kopiraj(aktivna);
                _original = Kopiraj(aktivna);
                Poruka = "firma.dbf je prazan — učitani su osnovni podaci.";
                return;
            }

            Firma = CitajSvaFirmaPolja(red, aktivna);
            _original = Kopiraj(Firma);
            _firmaSnapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(dbfPath);
            Poruka = "Podaci učitani iz firma.dbf.";
        }
        catch (Exception ex)
        {
            Firma = Kopiraj(aktivna);
            _original = Kopiraj(aktivna);
            Poruka = $"Greška pri čitanju firma.dbf: {ex.Message}";
        }
    }

    private static Firma CitajSvaFirmaPolja(Dictionary<string, object?> r, Firma baza)
    {
        static string S(Dictionary<string, object?> d, string k)
            => d.TryGetValue(k, out var v) && v is string s ? s : string.Empty;
        static DateTime? D(Dictionary<string, object?> d, string k)
            => d.TryGetValue(k, out var v) && v is DateTime dt ? dt : null;

        return new Firma
        {
            FolderPath           = baza.FolderPath,
            FolderIme            = baza.FolderIme,
            Aktivna              = baza.Aktivna,
            Naziv                = S(r, "FIME"),
            Naziv2               = S(r, "FIME2"),
            NazivLatinican       = S(r, "FIMEC"),
            Baza                 = S(r, "FBAZA"),
            Vlasnik              = S(r, "FVLAST"),
            OdgovornoLice        = S(r, "FOSOBA"),
            OrganizacioniOblik   = S(r, "FOBLIK"),
            Maticni              = S(r, "FMAT"),
            Pib                  = S(r, "FPOR"),
            PdvObveznik          = S(r, "FPDV"),
            SifraDelatnosti      = S(r, "FSIF"),
            NazivDelatnosti      = S(r, "FNAZD"),
            PostanskiBroj        = S(r, "FPOS"),
            Mesto                = S(r, "FMES"),
            Ulica                = S(r, "FUL"),
            BrojUlice            = S(r, "FULBR"),
            Opstina              = S(r, "FOPS"),
            Republika            = S(r, "FREPUB"),
            Drzava               = S(r, "FDRZAVA"),
            Telefon1             = S(r, "FTEL"),
            Telefon2             = S(r, "FTEL2"),
            Fax1                 = S(r, "FFAX"),
            Email                = S(r, "FEMAIL"),
            Web                  = S(r, "FVEB"),
            Agencija             = S(r, "FAGENC"),
            ZiroRacun            = S(r, "FZIRO"),
            ZiroRacun2           = S(r, "FZIRO2"),
            ZiroRacunDevizni     = S(r, "FZIRODEV"),
            ZiroRacunBolovanje   = S(r, "FZIROBOL"),
            Banka1               = S(r, "FBANKA"),
            Banka2               = S(r, "FBANKA2"),
            BankaDevizna         = S(r, "FBANKAD"),
            BankaBolovanje       = S(r, "FBANKAB"),
            SwiftKod             = S(r, "FSWIFT"),
            DatumOsnivanja       = D(r, "FDAT0"),
            DatumRegistracije    = D(r, "FDATREG"),
            DatumUpisa           = D(r, "FDATUPIS"),
            DatumPdv             = D(r, "FDATPDV"),
            RegBrojSocijalno     = S(r, "FREGSOC"),
            RegBrojZdravstveno   = S(r, "FREGZDR"),
            SudskiRegistar       = S(r, "FREGSUD"),
        };
    }

    private static string? PronadjiDbf(string folderPath, string fileName)
    {
        var exact = Path.Combine(folderPath, fileName);
        if (File.Exists(exact)) return exact;

        var upper = Path.Combine(folderPath, fileName.ToUpperInvariant());
        if (File.Exists(upper)) return upper;

        if (!Directory.Exists(folderPath)) return null;

        return Directory.GetFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<string, object?>? TryUcitajPostojeciRed(string putanja)
    {
        if (!File.Exists(putanja)) return null;

        var reader = new SimpleDbfReader(putanja);
        var zapis = reader.Zapisi().FirstOrDefault();
        if (zapis == null) return null;

        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in reader.Fields)
        {
            dict[field.Name] = field.Type switch
            {
                'D' => (object?)zapis.DajDate(field.Name),
                'N' or 'F' => (object?)zapis.DajDecimal(field.Name),
                'L' => (object?)zapis.DajBool(field.Name),
                _ => zapis.DajString(field.Name)
            };
        }
        return dict;
    }

    private static void AzurirajFirmaRed(Dictionary<string, object?> red, Firma f)
    {
        red["FIME"]    = f.Naziv;
        red["FIME2"]   = f.Naziv2;
        red["FIMEC"]   = f.NazivLatinican;
        red["FBAZA"]   = f.Baza;
        red["FPDV"]    = f.PdvObveznik;
        red["FSIF"]    = f.SifraDelatnosti;
        red["FNAZD"]   = f.NazivDelatnosti;
        red["FVLAST"]  = f.Vlasnik;
        red["FOSOBA"]  = f.OdgovornoLice;
        red["FOBLIK"]  = f.OrganizacioniOblik;
        red["FMAT"]    = f.Maticni;
        red["FPOR"]    = f.Pib;
        red["FPOS"]    = f.PostanskiBroj;
        red["FMES"]    = f.Mesto;
        red["FUL"]     = f.Ulica;
        red["FULBR"]   = f.BrojUlice;
        red["FOPS"]    = f.Opstina;
        red["FREPUB"]  = f.Republika;
        red["FDRZAVA"] = f.Drzava;
        red["FTEL"]    = f.Telefon1;
        red["FTEL2"]   = f.Telefon2;
        red["FFAX"]    = f.Fax1;
        red["FEMAIL"]  = f.Email;
        red["FVEB"]    = f.Web;
        red["FAGENC"]  = f.Agencija;
        red["FZIRO"]    = f.ZiroRacun;
        red["FZIRO2"]   = f.ZiroRacun2;
        red["FZIRODEV"] = f.ZiroRacunDevizni;
        red["FZIROBOL"] = f.ZiroRacunBolovanje;
        red["FBANKA"]   = f.Banka1;
        red["FBANKA2"]  = f.Banka2;
        red["FBANKAD"]  = f.BankaDevizna;
        red["FBANKAB"]  = f.BankaBolovanje;
        red["FSWIFT"]   = f.SwiftKod;
        red["FDAT0"]    = f.DatumOsnivanja;
        red["FDATREG"]  = f.DatumRegistracije;
        red["FDATUPIS"] = f.DatumUpisa;
        red["FDATPDV"]  = f.DatumPdv;
        red["FREGSOC"]  = f.RegBrojSocijalno;
        red["FREGZDR"]  = f.RegBrojZdravstveno;
        red["FREGSUD"]  = f.SudskiRegistar;
    }

    private static Firma Kopiraj(Firma source) => new()
    {
        FolderPath           = source.FolderPath,
        FolderIme            = source.FolderIme,
        Aktivna              = source.Aktivna,
        Naziv                = source.Naziv,
        Naziv2               = source.Naziv2,
        NazivLatinican       = source.NazivLatinican,
        Baza                 = source.Baza,
        PostanskiBroj        = source.PostanskiBroj,
        Mesto                = source.Mesto,
        Ulica                = source.Ulica,
        BrojUlice            = source.BrojUlice,
        Republika            = source.Republika,
        Drzava               = source.Drzava,
        Opstina              = source.Opstina,
        Telefon1             = source.Telefon1,
        Telefon2             = source.Telefon2,
        Fax1                 = source.Fax1,
        Email                = source.Email,
        Web                  = source.Web,
        Agencija             = source.Agencija,
        ZiroRacun            = source.ZiroRacun,
        ZiroRacun2           = source.ZiroRacun2,
        ZiroRacunDevizni     = source.ZiroRacunDevizni,
        ZiroRacunBolovanje   = source.ZiroRacunBolovanje,
        Banka1               = source.Banka1,
        Banka2               = source.Banka2,
        BankaDevizna         = source.BankaDevizna,
        BankaBolovanje       = source.BankaBolovanje,
        SwiftKod             = source.SwiftKod,
        SifraDelatnosti      = source.SifraDelatnosti,
        NazivDelatnosti      = source.NazivDelatnosti,
        Maticni              = source.Maticni,
        MatBr                = source.MatBr,
        Pib                  = source.Pib,
        PdvObveznik          = source.PdvObveznik,
        OrganizacioniOblik   = source.OrganizacioniOblik,
        Vlasnik              = source.Vlasnik,
        OdgovornoLice        = source.OdgovornoLice,
        DatumOsnivanja       = source.DatumOsnivanja,
        DatumRegistracije    = source.DatumRegistracije,
        DatumUpisa           = source.DatumUpisa,
        DatumPdv             = source.DatumPdv,
        RegBrojSocijalno     = source.RegBrojSocijalno,
        RegBrojZdravstveno   = source.RegBrojZdravstveno,
        SudskiRegistar       = source.SudskiRegistar,
    };

    [RelayCommand]
    private void Osvezi() => Ucitaj();
}

internal static class FirmaPodaciExtensions
{
    public static void Let<T>(this T? value, Action<T> action) where T : class
    {
        if (value != null) action(value);
    }
}
