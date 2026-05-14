using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class FirmaIzborViewModel : ObservableObject
{
    private readonly IFirmaService _firmaService;
    private readonly IPutanjaService _putanjaService;
    private readonly AppState _appState;

    public event Action? FirmaIzabrana;
    public event Action? OtkacenoPrijavljeni;

    public FirmaIzborViewModel(IFirmaService firmaService, IPutanjaService putanjaService, AppState appState)
    {
        _firmaService = firmaService;
        _putanjaService = putanjaService;
        _appState = appState;
    }

    [ObservableProperty]
    private ObservableCollection<Firma> _firme = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PotvrdiCommand))]
    [NotifyCanExecuteChangedFor(nameof(ObrisiFirmuCommand))]
    [NotifyCanExecuteChangedFor(nameof(ArhivirajFirmuCommand))]
    private Firma? _selektovanaFirma;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PotvrdiCommand))]
    [NotifyCanExecuteChangedFor(nameof(DodajFirmuCommand))]
    [NotifyCanExecuteChangedFor(nameof(ObrisiFirmuCommand))]
    [NotifyCanExecuteChangedFor(nameof(ArhivirajFirmuCommand))]
    private bool _ucitava = true;

    [ObservableProperty]
    private string _statusPoruka = string.Empty;

    public string KorisnikInfo =>
        $"Prijavljeni ste kao: {_appState.TrenutniKorisnik?.KorisnikIme ?? "—"}";

    public async Task UcitajAsync()
    {
        Ucitava = true;
        Log.Debug("FirmaIzbor — učitavam firme...");
        var lista = await _firmaService.DajSveFirmeAsync();
        Log.Debug("FirmaIzbor — učitano {Count} firmi", lista.Count);
        Firme = new ObservableCollection<Firma>(lista);
        SelektovanaFirma = lista.FirstOrDefault(f => f.Aktivna) ?? lista.FirstOrDefault();
        Ucitava = false;
    }

    private bool MozePotvrdi() => SelektovanaFirma != null && !Ucitava;
    private bool MozeBrisiIliArhivuj() => SelektovanaFirma != null && !Ucitava;
    private bool MozeDodaj() => !Ucitava;

    [RelayCommand(CanExecute = nameof(MozePotvrdi))]
    private void Potvrdi()
    {
        if (SelektovanaFirma is null) return;
        Log.Information("Firma izabrana: {Naziv} ({Folder})", SelektovanaFirma.Naziv, SelektovanaFirma.FolderPath);
        _appState.PostaviFirmu(SelektovanaFirma);
        FirmaIzabrana?.Invoke();
    }

    [RelayCommand]
    private void Otkaci()
    {
        _appState.Odjavi();
        OtkacenoPrijavljeni?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(MozeDodaj))]
    private async Task DodajFirmu()
    {
        Ucitava = true;
        StatusPoruka = "Kreiram novu firmu...";
        try
        {
            var novaFirma = await _firmaService.DodajFirmuAsync();
            await UcitajAsync();

            if (novaFirma is null)
            {
                StatusPoruka = "Firma nije dodata.";
                return;
            }

            SelektovanaFirma = Firme.FirstOrDefault(f =>
                string.Equals(f.FolderPath, novaFirma.FolderPath, StringComparison.OrdinalIgnoreCase));

            StatusPoruka = $"Kreirana je nova firma: {Path.GetFileName(novaFirma.FolderPath)}";
            MessageBox.Show(
                $"Kreirana je nova firma u folderu {Path.GetFileName(novaFirma.FolderPath)}.",
                "Dodavanje firme", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DodajFirmu — greška");
            StatusPoruka = $"Greška: {ex.Message}";
        }
        finally { Ucitava = false; }
    }

    [RelayCommand(CanExecute = nameof(MozeBrisiIliArhivuj))]
    private async Task ObrisiFirmu()
    {
        if (SelektovanaFirma is null) return;

        var potvrda = MessageBox.Show(
            $"Da li ste sigurni da želite da obrišete firmu:\n\n\"{SelektovanaFirma.Naziv}\"\n\n" +
            $"Folder: {SelektovanaFirma.FolderPath}\n\nOva akcija je NEPOVRATNA!",
            "Brisanje firme", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

        if (potvrda != MessageBoxResult.Yes) return;

        Ucitava = true;
        StatusPoruka = $"Brišem firmu \"{SelektovanaFirma.Naziv}\"...";
        try
        {
            var uspelo = await _firmaService.ObrisiFirmuAsync(SelektovanaFirma.FolderPath);
            if (!uspelo)
            {
                StatusPoruka = "Brisanje nije uspelo.";
                MessageBox.Show("Brisanje firme nije uspelo. Provjerite da li su fajlovi zaključani.",
                    "Brisanje firme", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            await UcitajAsync();
            StatusPoruka = "Firma je obrisana.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ObrisiFirmu — greška");
            StatusPoruka = $"Greška: {ex.Message}";
        }
        finally { Ucitava = false; }
    }

    [RelayCommand(CanExecute = nameof(MozeBrisiIliArhivuj))]
    private async Task ArhivirajFirmu()
    {
        if (SelektovanaFirma is null) return;

        var arhivaPutanja = _putanjaService.DajArhivaPutanju();
        if (string.IsNullOrWhiteSpace(arhivaPutanja))
        {
            MessageBox.Show(
                "Putanja za arhiviranje nije postavljena.\nPostavite je na početnom ekranu.",
                "Arhiviranje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var potvrda = MessageBox.Show(
            $"Arhivirati podatke za firmu:\n\n\"{SelektovanaFirma.Naziv}\"\n\nArhiva će biti u:\n{arhivaPutanja}",
            "Arhiviranje", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

        if (potvrda != MessageBoxResult.Yes) return;

        Ucitava = true;
        StatusPoruka = "Arhiviranje u toku...";
        try
        {
            var firma = SelektovanaFirma;
            var zipPath = await Task.Run(() => KreirajArhivu(firma.FolderPath, arhivaPutanja));
            StatusPoruka = $"Arhiva sačuvana: {Path.GetFileName(zipPath)}";
            MessageBox.Show($"Arhiviranje završeno.\n\nFajl: {zipPath}",
                "Arhiviranje završeno", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ArhivirajFirmu — greška");
            StatusPoruka = $"Greška: {ex.Message}";
        }
        finally { Ucitava = false; }
    }

    private static string KreirajArhivu(string firmaFolder, string arhivaPutanja)
    {
        Directory.CreateDirectory(arhivaPutanja);
        var folderName = Path.GetFileName(firmaFolder.TrimEnd(Path.DirectorySeparatorChar));
        var timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
        var zipPath = Path.Combine(arhivaPutanja, $"{folderName}_{timestamp}.zip");

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        DodajFolderUZip(zip, firmaFolder, folderName);
        return zipPath;
    }

    private static void DodajFolderUZip(ZipArchive zip, string sourceFolder, string prefix)
    {
        foreach (var fajl in Directory.GetFiles(sourceFolder))
        {
            var entry = zip.CreateEntry($"{prefix}/{Path.GetFileName(fajl)}", CompressionLevel.Optimal);
            using var src = new FileStream(fajl, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var dst = entry.Open();
            src.CopyTo(dst);
        }

        foreach (var sub in Directory.GetDirectories(sourceFolder))
            DodajFolderUZip(zip, sub, $"{prefix}/{Path.GetFileName(sub)}");
    }
}
