using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;
using Serilog;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class PartneriViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext<PartneriViewModel>();
    private readonly string _folderPath;
    private DbfOptimisticConcurrency.FileSnapshot? _snapshot;

    [ObservableProperty] private ObservableCollection<PartnerStavka> _stavke = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(IzmeniCommand))]
    [NotifyCanExecuteChangedFor(nameof(ObrisiCommand))]
    [NotifyCanExecuteChangedFor(nameof(KopirajRedCommand))]
    private PartnerStavka? _selektovani;
    [ObservableProperty] private string _pretraga = string.Empty;
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private bool _ucitava;

    public PartneriViewModel(AppState appState)
    {
        _folderPath = appState.AktivnaFirma?.FolderPath ?? string.Empty;
        Ucitaj();
    }

    [RelayCommand]
    private void Ucitaj()
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Stavke = [];
            Selektovani = null;
            Poruka = "Nije izabrana firma.";
            return;
        }

        var an0Path = PartneriDbfSupport.PronadjiDbf(_folderPath, "an0.dbf");
        if (an0Path != null)
            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(an0Path);

        Ucitava = true;
        try
        {
            var lista = PartneriDbfSupport.UcitajPartnere(_folderPath)
                .OrderBy(x => ParseSifraZaSort(x.Sifra))
                .ThenBy(x => x.Naziv, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Stavke = new ObservableCollection<PartnerStavka>(lista);
            Selektovani = Stavke.FirstOrDefault();
            Poruka = $"Učitano {Stavke.Count} partnera iz an0.dbf.";
            _log.Debug("an0.dbf: učitano {Count} partnera", Stavke.Count);
        }
        catch (Exception ex)
        {
            Stavke = [];
            Poruka = $"Greška pri učitavanju partnera: {ex.Message}";
            _log.Error(ex, "Greška pri učitavanju an0.dbf");
        }
        finally { Ucitava = false; }
    }

    [RelayCommand]
    private void Dodaj()
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var novaSifra = PredloziNovuSifru();
        var nova = new PartnerStavka { Sifra = novaSifra, Pib2 = novaSifra, Drzava = "SRBIJA", Preneto = " " };

        var vm = new PartnerKarticaViewModel(nova, noviUnos: true);
        var view = new PartnerKarticaWindow(vm);
        if (view.ShowDialog() != true) return;

        Stavke.Add(vm.Stavka);
        Selektovani = vm.Stavka;
        Poruka = $"Dodat je novi partner šifra {vm.Stavka.Sifra}. Kliknite SAČUVAJ.";
    }

    private bool MozeIzmeni() => Selektovani != null;

    [RelayCommand(CanExecute = nameof(MozeIzmeni))]
    private void Izmeni()
    {
        if (Selektovani == null) return;

        var kopija = Selektovani.Clone();
        var vm = new PartnerKarticaViewModel(kopija, noviUnos: false);
        var view = new PartnerKarticaWindow(vm);
        if (view.ShowDialog() != true) return;

        Selektovani.CopyFrom(vm.Stavka);
        Poruka = $"Ažuriran je partner {Selektovani.Sifra}. Kliknite SAČUVAJ.";
    }

    private bool MozeObrisi() => Selektovani != null;

    [RelayCommand(CanExecute = nameof(MozeObrisi))]
    private void Obrisi()
    {
        if (Selektovani == null) return;
        var uklonjen = Selektovani;
        Stavke.Remove(uklonjen);
        Selektovani = Stavke.FirstOrDefault();
        Poruka = $"Obrisan je partner {uklonjen.Sifra}. Kliknite SAČUVAJ.";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var an0Path = PartneriDbfSupport.PronadjiDbf(_folderPath, "an0.dbf");
        if (_snapshot != null && an0Path != null && DbfOptimisticConcurrency.HasFileChanged(an0Path, _snapshot))
        {
            var r = MessageBox.Show(
                "Fajl an0.dbf je izmenjen od strane drugog korisnika.\nNastaviti sa čuvanjem (prepisati)?",
                "Upozorenje — dual korisnici", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes) return;
        }

        try
        {
            PartneriDbfSupport.SacuvajPartnere(_folderPath, Stavke);
            if (an0Path != null)
                _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(an0Path);
            Poruka = $"Sačuvano {Stavke.Count} partnera u an0.dbf.";
            _log.Information("an0.dbf: sačuvano {Count} partnera", Stavke.Count);
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čuvanju partnera: {ex.Message}";
            _log.Error(ex, "Greška pri snimanju an0.dbf");
        }
    }

    [RelayCommand]
    private void TraziSifru()
    {
        var upit = (Pretraga ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(upit)) { Poruka = "Unesite šifru partnera za pretragu."; return; }
        var nadjen = Stavke.FirstOrDefault(x => x.Sifra.Equals(upit, StringComparison.OrdinalIgnoreCase));
        if (nadjen == null) { Poruka = $"Partner sa šifrom {upit} nije pronađen."; return; }
        Selektovani = nadjen;
        Poruka = $"Pronađen partner sa šifrom {upit}.";
    }

    [RelayCommand]
    private void TraziNaziv()
    {
        var upit = (Pretraga ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(upit)) { Poruka = "Unesite naziv partnera za pretragu."; return; }
        var nadjen = Stavke.FirstOrDefault(x => x.Naziv.Contains(upit, StringComparison.OrdinalIgnoreCase));
        if (nadjen == null) { Poruka = $"Partner sa nazivom \"{upit}\" nije pronađen."; return; }
        Selektovani = nadjen;
        Poruka = $"Pronađen partner {nadjen.Naziv}.";
    }

    [RelayCommand(CanExecute = nameof(MozeIzmeni))]
    private void KopirajRed()
    {
        if (Selektovani == null) return;
        var kopija = Selektovani.Clone();
        kopija.Sifra = PredloziNovuSifru();
        kopija.Pib2 = kopija.Sifra;

        var vm = new PartnerKarticaViewModel(kopija, noviUnos: true);
        var view = new PartnerKarticaWindow(vm);
        if (view.ShowDialog() != true) return;

        Stavke.Add(vm.Stavka);
        Selektovani = vm.Stavka;
        Poruka = $"Kopiran partner šifra {vm.Stavka.Sifra}. Kliknite SAČUVAJ.";
    }

    [RelayCommand]
    private void ExportUExcel()
    {
        if (Stavke.Count == 0) { Poruka = "Nema partnera za export."; return; }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export partnera u Excel",
            Filter = "CSV fajl (*.csv)|*.csv",
            FileName = $"partneri_{DateTime.Today:yyyyMMdd}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Sifra;Naziv;Naziv2;Posta;Mesto;Ulica;Broj;Telefon;Kontakt;PIB;Maticni;Email;Ziro racun");
            foreach (var p in Stavke)
                sb.AppendLine(string.Join(";", p.Sifra, p.Naziv, p.Naziv2, p.Posta, p.Mesto,
                    p.Ulica, p.Ulbroj, p.Telefon, p.Lice1, p.Pib, p.Maticni, p.Email, p.ZiroRac));

            System.IO.File.WriteAllText(dlg.FileName, sb.ToString(),
                new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            Poruka = $"Eksportovano {Stavke.Count} partnera → {System.IO.Path.GetFileName(dlg.FileName)}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { Poruka = $"Greška pri eksportu: {ex.Message}"; }
    }

    private string PredloziNovuSifru()
    {
        var max = Stavke.Select(x => ParseSifraZaNoviBroj(x.Sifra)).Where(x => x > 0).DefaultIfEmpty(0).Max();
        return (max + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static int ParseSifraZaSort(string sifra)
        => int.TryParse((sifra ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var broj) ? broj : int.MaxValue;

    private static int ParseSifraZaNoviBroj(string sifra)
        => int.TryParse((sifra ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var broj) ? broj : 0;
}

public partial class PartnerStavka : ObservableObject
{
    private readonly Dictionary<string, object?> _originalValues = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty] private string _sifra = string.Empty;
    [ObservableProperty] private string _naziv = string.Empty;
    [ObservableProperty] private string _naziv2 = string.Empty;
    [ObservableProperty] private string _posta = string.Empty;
    [ObservableProperty] private string _mesto = string.Empty;
    [ObservableProperty] private string _ulica = string.Empty;
    [ObservableProperty] private string _ulbroj = string.Empty;
    [ObservableProperty] private string _telefon = string.Empty;
    [ObservableProperty] private string _telefon2 = string.Empty;
    [ObservableProperty] private string _fax = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _lice1 = string.Empty;
    [ObservableProperty] private string _telLice1 = string.Empty;
    [ObservableProperty] private string _pib = string.Empty;
    [ObservableProperty] private string _pib2 = string.Empty;
    [ObservableProperty] private string _maticni = string.Empty;
    [ObservableProperty] private string _ziroRac = string.Empty;
    [ObservableProperty] private string _drzava = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private long _idBr;

    public void SetOriginalValues(Dictionary<string, object?> values)
    {
        _originalValues.Clear();
        foreach (var kv in values) _originalValues[kv.Key] = kv.Value;
    }

    public bool TryGetOriginalValue(string fieldName, out object? value)
        => _originalValues.TryGetValue(fieldName, out value);

    public PartnerStavka Clone()
    {
        var clone = new PartnerStavka
        {
            Sifra = Sifra, Naziv = Naziv, Naziv2 = Naziv2, Posta = Posta, Mesto = Mesto,
            Ulica = Ulica, Ulbroj = Ulbroj, Telefon = Telefon, Telefon2 = Telefon2, Fax = Fax,
            Email = Email, Lice1 = Lice1, TelLice1 = TelLice1, Pib = Pib, Pib2 = Pib2,
            Maticni = Maticni, ZiroRac = ZiroRac, Drzava = Drzava, Preneto = Preneto, IdBr = IdBr
        };
        foreach (var kv in _originalValues) clone._originalValues[kv.Key] = kv.Value;
        return clone;
    }

    public void CopyFrom(PartnerStavka other)
    {
        Sifra = other.Sifra; Naziv = other.Naziv; Naziv2 = other.Naziv2; Posta = other.Posta;
        Mesto = other.Mesto; Ulica = other.Ulica; Ulbroj = other.Ulbroj; Telefon = other.Telefon;
        Telefon2 = other.Telefon2; Fax = other.Fax; Email = other.Email; Lice1 = other.Lice1;
        TelLice1 = other.TelLice1; Pib = other.Pib; Pib2 = other.Pib2; Maticni = other.Maticni;
        ZiroRac = other.ZiroRac; Drzava = other.Drzava; Preneto = other.Preneto; IdBr = other.IdBr;
        _originalValues.Clear();
        foreach (var kv in other._originalValues) _originalValues[kv.Key] = kv.Value;
    }
}
