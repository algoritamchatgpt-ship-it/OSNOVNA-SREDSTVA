using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class OsKarticaLookupViewModel : ObservableObject
{
    private sealed record LookupConfig(
        string Naslov,
        string HintPretraga,
        string[] DbfKandidati,
        string SifraPolje,
        string NazivPolje,
        string? DodatnoPolje,
        string SifraNaslov,
        string NazivNaslov,
        string DodatnoNaslov);

    private readonly AppState _appState;
    private readonly LookupConfig _config;
    private readonly List<OsKarticaLookupStavka> _sveStavke = [];

    private string? _dbfPath;
    private string _inicijalnaSifra = string.Empty;

    [ObservableProperty] private string _naslov = string.Empty;
    [ObservableProperty] private string _hintPretraga = string.Empty;
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private string _pretraga = string.Empty;

    [ObservableProperty] private string _sifraNaslov = "SIFRA";
    [ObservableProperty] private string _nazivNaslov = "NAZIV";
    [ObservableProperty] private string _dodatnoNaslov = string.Empty;
    [ObservableProperty] private bool _prikaziDodatno;

    [ObservableProperty] private ObservableCollection<OsKarticaLookupStavka> _stavke = [];
    [ObservableProperty] private OsKarticaLookupStavka? _izabranaStavka;

    public string IzabranaSifra => IzabranaStavka?.Sifra?.Trim() ?? string.Empty;
    public string IzabraniNaziv => IzabranaStavka?.Naziv?.Trim() ?? string.Empty;

    public OsKarticaLookupViewModel(AppState appState, OsKarticaLookupTip tip, string? inicijalnaSifra)
    {
        _appState = appState;
        _config = KreirajConfig(tip);
        _inicijalnaSifra = (inicijalnaSifra ?? string.Empty).Trim();

        Naslov = _config.Naslov;
        HintPretraga = _config.HintPretraga;
        SifraNaslov = _config.SifraNaslov;
        NazivNaslov = _config.NazivNaslov;
        DodatnoNaslov = _config.DodatnoNaslov;
        PrikaziDodatno = !string.IsNullOrWhiteSpace(_config.DodatnoPolje);

        Ucitaj();
    }

    partial void OnPretragaChanged(string value) => PrimeniFilter();

    [RelayCommand]
    private void Dodaj()
    {
        if (string.IsNullOrWhiteSpace(_dbfPath))
        {
            Poruka = "Tabela nije pronađena.";
            return;
        }

        var nova = new OsKarticaLookupStavka
        {
            Sifra = PredloziNovuSifru(),
            Naziv = string.Empty,
            Dodatno = string.Empty
        };

        if (string.Equals(_config.SifraPolje, "KONTO", StringComparison.OrdinalIgnoreCase))
            nova.Sifra = string.Empty;

        _sveStavke.Add(nova);
        PrimeniFilter();
        IzabranaStavka = nova;
        Poruka = "Dodat je novi red. Unesite podatke u polja ispod, pa Sačuvaj.";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        SacuvajSve(prikaziPorukuNaUspeh: true);
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Izaberi(Window? window)
    {
        if (IzabranaStavka == null)
        {
            Poruka = "Nije izabran red.";
            return;
        }

        if (SacuvajSve(prikaziPorukuNaUspeh: false) && window != null)
            window.DialogResult = true;
    }

    [RelayCommand]
    private void Otkazi(Window? window)
    {
        if (window == null)
            return;

        if (IzabranaStavka != null)
        {
            if (SacuvajSve(prikaziPorukuNaUspeh: false))
                window.DialogResult = true;

            return;
        }

        window.DialogResult = false;
    }

    private bool SacuvajSve(bool prikaziPorukuNaUspeh)
    {
        if (string.IsNullOrWhiteSpace(_dbfPath))
        {
            Poruka = "Tabela nije pronađena.";
            return false;
        }

        if (!ValidirajStavke())
            return false;

        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            DbfTableWriter.WriteTable(_dbfPath, schema, _sveStavke, ResolveFieldValue);
            if (prikaziPorukuNaUspeh)
                Poruka = $"Sačuvano {_sveStavke.Count} zapisa.";

            return true;
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri cuvanju: {ex.Message}";
            return false;
        }
    }

    private void Ucitaj()
    {
        _dbfPath = PronadjiDbf(_config.DbfKandidati);
        _sveStavke.Clear();

        if (string.IsNullOrWhiteSpace(_dbfPath))
        {
            Stavke = [];
            Poruka = $"Tabela nije pronađena: {string.Join(", ", _config.DbfKandidati)}";
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(_dbfPath);
            foreach (var z in zapisi)
            {
                var red = new OsKarticaLookupStavka
                {
                    Sifra = DbfReader.Str(z, _config.SifraPolje),
                    Naziv = DbfReader.Str(z, _config.NazivPolje),
                    Dodatno = string.IsNullOrWhiteSpace(_config.DodatnoPolje) ? string.Empty : DbfReader.Str(z, _config.DodatnoPolje)
                };

                foreach (var kv in z)
                    red.OriginalnaPolja[kv.Key] = kv.Value;

                _sveStavke.Add(red);
            }

            PrimeniFilter();
            OznaciInicijalniRed();
            Poruka = $"Učitano {_sveStavke.Count} zapisa.";
        }
        catch (Exception ex)
        {
            Stavke = [];
            Poruka = $"Greška pri ucitavanju: {ex.Message}";
        }
    }

    private void PrimeniFilter()
    {
        var upit = (Pretraga ?? string.Empty).Trim();
        IEnumerable<OsKarticaLookupStavka> filtrirano = _sveStavke;

        if (!string.IsNullOrWhiteSpace(upit))
        {
            filtrirano = _sveStavke.Where(s =>
                (s.Sifra ?? string.Empty).Contains(upit, StringComparison.OrdinalIgnoreCase) ||
                (s.Naziv ?? string.Empty).Contains(upit, StringComparison.OrdinalIgnoreCase) ||
                (s.Dodatno ?? string.Empty).Contains(upit, StringComparison.OrdinalIgnoreCase));
        }

        var prethodnaSifra = IzabranaStavka?.Sifra?.Trim() ?? string.Empty;
        Stavke = new ObservableCollection<OsKarticaLookupStavka>(filtrirano);

        if (!string.IsNullOrWhiteSpace(prethodnaSifra))
        {
            IzabranaStavka = Stavke.FirstOrDefault(s =>
                string.Equals((s.Sifra ?? string.Empty).Trim(), prethodnaSifra, StringComparison.OrdinalIgnoreCase));
        }

        IzabranaStavka ??= Stavke.FirstOrDefault();
    }

    private bool ValidirajStavke()
    {
        for (var i = 0; i < _sveStavke.Count; i++)
        {
            var red = _sveStavke[i];
            if (string.IsNullOrWhiteSpace(red.Sifra))
            {
                MessageBox.Show($"Red {i + 1}: polje {_config.SifraPolje} je obavezno.",
                    Naslov, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(red.Naziv))
            {
                MessageBox.Show($"Red {i + 1}: polje {_config.NazivPolje} je obavezno.",
                    Naslov, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        return true;
    }

    private object? ResolveFieldValue(OsKarticaLookupStavka red, string dbfPolje)
    {
        if (dbfPolje.Equals(_config.SifraPolje, StringComparison.OrdinalIgnoreCase))
            return red.Sifra.Trim();

        if (dbfPolje.Equals(_config.NazivPolje, StringComparison.OrdinalIgnoreCase))
            return red.Naziv.Trim();

        if (!string.IsNullOrWhiteSpace(_config.DodatnoPolje) &&
            dbfPolje.Equals(_config.DodatnoPolje, StringComparison.OrdinalIgnoreCase))
            return red.Dodatno.Trim();

        return red.OriginalnaPolja.TryGetValue(dbfPolje, out var v) ? v : null;
    }

    private void OznaciInicijalniRed()
    {
        if (Stavke.Count == 0)
            return;

        if (!string.IsNullOrWhiteSpace(_inicijalnaSifra))
        {
            IzabranaStavka = Stavke.FirstOrDefault(s =>
                string.Equals((s.Sifra ?? string.Empty).Trim(), _inicijalnaSifra, StringComparison.OrdinalIgnoreCase));
        }

        IzabranaStavka ??= Stavke.FirstOrDefault();
    }

    private string PredloziNovuSifru()
    {
        var max = 0;
        foreach (var s in _sveStavke)
        {
            var raw = (s.Sifra ?? string.Empty).Trim();
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var broj) && broj > max)
                max = broj;
        }

        return (max + 1).ToString(CultureInfo.InvariantCulture);
    }

    private string? PronadjiDbf(IEnumerable<string> kandidati)
    {
        foreach (var ime in kandidati)
        {
            var hit = DbfHelper.NadjiDbf(_appState, ime);
            if (hit != null) return hit;
        }
        return null;
    }

    private static LookupConfig KreirajConfig(OsKarticaLookupTip tip) => tip switch
    {
        OsKarticaLookupTip.Vrsta => new LookupConfig(
            Naslov: "OS kartica - izbor vrste",
            HintPretraga: "Pretraga po sifri ili nazivu vrste...",
            DbfKandidati: ["osvrsta.dbf"],
            SifraPolje: "VRSTA",
            NazivPolje: "NAZIV",
            DodatnoPolje: null,
            SifraNaslov: "Vrsta",
            NazivNaslov: "Naziv vrste sredstva",
            DodatnoNaslov: string.Empty),

        OsKarticaLookupTip.OsnovKoriscenja => new LookupConfig(
            Naslov: "OS kartica - osnov koriscenja",
            HintPretraga: "Pretraga po sifri ili nazivu osnova...",
            DbfKandidati: ["ososnk.dbf"],
            SifraPolje: "OSNOVKOR",
            NazivPolje: "NAZIV",
            DodatnoPolje: null,
            SifraNaslov: "Osnov koriscenja",
            NazivNaslov: "Naziv osnova",
            DodatnoNaslov: string.Empty),

        OsKarticaLookupTip.IzvorFinansiranja => new LookupConfig(
            Naslov: "OS kartica - izvor finansiranja",
            HintPretraga: "Pretraga po sifri ili nazivu izvora...",
            DbfKandidati: ["osizvorf.dbf"],
            SifraPolje: "IZVOR",
            NazivPolje: "NAZIV",
            DodatnoPolje: null,
            SifraNaslov: "Izvor",
            NazivNaslov: "Naziv izvora",
            DodatnoNaslov: string.Empty),

        OsKarticaLookupTip.AmortizacionaGrupa => new LookupConfig(
            Naslov: "OS kartica - amortizaciona grupa",
            HintPretraga: "Pretraga po AG ili opisu...",
            DbfKandidati: ["osag.dbf"],
            SifraPolje: "AG",
            NazivPolje: "OPIS",
            DodatnoPolje: "VRSTA",
            SifraNaslov: "AG",
            NazivNaslov: "Opis grupe",
            DodatnoNaslov: "VRSTA"),

        OsKarticaLookupTip.AmortizacionaPodgrupa => new LookupConfig(
            Naslov: "OS kartica - amortizaciona podgrupa",
            HintPretraga: "Pretraga po AGPOD ili opisu...",
            DbfKandidati: ["osagpod.dbf"],
            SifraPolje: "AGPOD",
            NazivPolje: "OPIS",
            DodatnoPolje: "AG",
            SifraNaslov: "AGPOD",
            NazivNaslov: "Opis podgrupe",
            DodatnoNaslov: "AG"),

        OsKarticaLookupTip.Mesto => new LookupConfig(
            Naslov: "OS kartica - mesto",
            HintPretraga: "Pretraga po sifri mesta, posti ili nazivu...",
            DbfKandidati: ["mesta.dbf"],
            SifraPolje: "MP",
            NazivPolje: "MESTO",
            DodatnoPolje: "POSTA",
            SifraNaslov: "Sifra mesta",
            NazivNaslov: "Naziv mesta",
            DodatnoNaslov: "POSTA"),

        OsKarticaLookupTip.Konto => new LookupConfig(
            Naslov: "OS kartica - konto",
            HintPretraga: "Pretraga po kontu ili opisu...",
            DbfKandidati: ["konto.dbf"],
            SifraPolje: "KONTO",
            NazivPolje: "OPIS",
            DodatnoPolje: null,
            SifraNaslov: "Konto",
            NazivNaslov: "Naziv konta",
            DodatnoNaslov: string.Empty),

        _ => throw new ArgumentOutOfRangeException(nameof(tip), tip, null)
    };
}
