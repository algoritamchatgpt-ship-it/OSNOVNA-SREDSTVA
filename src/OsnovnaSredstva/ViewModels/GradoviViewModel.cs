using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using System.Collections.ObjectModel;
using System.IO;

namespace OsnovnaSredstva.ViewModels;

public partial class GradoviViewModel : ObservableObject
{
    private readonly string _finRootFolder;
    private readonly string _firmaFolderPath;
    private List<GradoviStavka> _sveStavke = [];

    [ObservableProperty] private ObservableCollection<GradoviStavka> _stavke = [];
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private string _pretragaMesta = string.Empty;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PopuniCommand))]
    [NotifyCanExecuteChangedFor(nameof(IsprazniCommand))]
    private bool _ucitava = true;

    public GradoviViewModel(AppState appState, IPutanjaService putanjaService)
    {
        _firmaFolderPath = appState.AktivnaFirma?.FolderPath ?? string.Empty;
        _finRootFolder = putanjaService.DajFinPutanju() ?? string.Empty;
        UcitajPodatke();
    }

    partial void OnPretragaMestaChanged(string value) => PrimeniFilter();

    private bool MozePopuni() => !Ucitava;
    private bool MozeIsprazni() => !Ucitava;

    [RelayCommand(CanExecute = nameof(MozeIsprazni))]
    private void Isprazni()
    {
        var targetDbf = OdrediCiljnuMestaTabelu();
        if (targetDbf is null)
        {
            Poruka = "Nije pronađena ciljna tabela MESTA za aktivnu firmu.";
            return;
        }

        try
        {
            IsprazniDbfSadrzaj(targetDbf);
            ObrisiIndeksAkoPostoji(targetDbf);
            UcitajPodatke();
            Poruka = $"Tabela je ispraznjena: {targetDbf}";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri pražnjenju MESTA: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(MozePopuni))]
    private void Popuni()
    {
        var targetDbf = OdrediCiljnuMestaTabelu();
        if (targetDbf is null)
        {
            Poruka = "Nije pronađena ciljna tabela MESTA za aktivnu firmu.";
            return;
        }

        try
        {
            var postojeci = DbfReader.CitajSveZapise(targetDbf);
            if (postojeci.Count > 0)
            {
                Poruka = $"Tabela nije prazna ({postojeci.Count} zapisa). Prvo klikni ISPRAZNI.";
                return;
            }

            var sourceDbf = PronadjiPunuMestaTabelu(targetDbf);
            if (sourceDbf is null)
            {
                Poruka = "Puna tabela MESTA nije pronađena.";
                return;
            }

            KopirajMestaDatoteke(sourceDbf, targetDbf);
            UcitajPodatke();
            Poruka = $"Tabela je popunjena iz: {sourceDbf}";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri popunjavanju MESTA: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OcistiPretragu() => PretragaMesta = string.Empty;

    [RelayCommand]
    private void Osvezi() => UcitajPodatke();

    private void UcitajPodatke()
    {
        Ucitava = true;
        _sveStavke = [];
        Stavke = [];

        if (string.IsNullOrWhiteSpace(_finRootFolder) && string.IsNullOrWhiteSpace(_firmaFolderPath))
        {
            Poruka = "Nije izabrana firma.";
            Ucitava = false;
            return;
        }

        var dbfPath = OdrediCiljnuMestaTabelu();

        if (dbfPath is null)
        {
            Poruka = "Tabela MESTA za aktivnu firmu nije pronađena.";
            Ucitava = false;
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);

            if (zapisi.Count == 0)
            {
                Poruka = "Tabela MESTA je prazna.";
            }
            else
            {
                foreach (var z in zapisi)
                {
                    _sveStavke.Add(new GradoviStavka
                    {
                        Mp = DbfReader.Str(z, "MP"),
                        Posta = DbfReader.Str(z, "POSTA"),
                        Mesto = DbfReader.Str(z, "MESTO"),
                        Ziro1 = DbfReader.Str(z, "ZIRO1"),
                        Ziro2 = DbfReader.Str(z, "ZIRO2"),
                        PorBroj = DbfReader.Str(z, "PORBROJ"),
                        PorBrojP = DbfReader.Str(z, "PORBROJP"),
                        RegSoc = DbfReader.Str(z, "REGSOC"),
                        Por = DbfReader.Dec(z, "POR"),
                        Zdr = DbfReader.Dec(z, "ZDR"),
                        Pio = DbfReader.Dec(z, "PIO"),
                        Nez = DbfReader.Dec(z, "NEZ"),
                        Vrsta = DbfReader.Str(z, "VRSTA"),
                        Preneto = DbfReader.Str(z, "PRENETO"),
                    });
                }
                PrimeniFilter();
            }
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čitanju: {ex.Message}";
        }

        Ucitava = false;
    }

    private void PrimeniFilter()
    {
        var upit = (PretragaMesta ?? string.Empty).Trim();

        IEnumerable<GradoviStavka> filtrirano = _sveStavke;
        if (!string.IsNullOrWhiteSpace(upit))
            filtrirano = _sveStavke.Where(s => s.Mesto.Contains(upit, StringComparison.OrdinalIgnoreCase));

        Stavke = new ObservableCollection<GradoviStavka>(filtrirano);

        Poruka = string.IsNullOrWhiteSpace(upit)
            ? $"Učitano {_sveStavke.Count} mesta."
            : $"Pronađeno {Stavke.Count} mesta za \"{upit}\".";
    }

    private string? OdrediCiljnuMestaTabelu()
    {
        if (!string.IsNullOrWhiteSpace(_firmaFolderPath))
        {
            var izFirme = PronadjiMestaDbfUFolderu(_firmaFolderPath);
            if (izFirme is not null) return izFirme;
        }

        if (!string.IsNullOrWhiteSpace(_finRootFolder))
        {
            var izRoota = PronadjiMestaDbfUFolderu(_finRootFolder);
            if (izRoota is not null) return izRoota;
        }

        return PronadjiMestaDbf(_finRootFolder);
    }

    private string? PronadjiPunuMestaTabelu(string targetDbfPath)
    {
        var candidates = new List<string>
        {
            Path.Combine(_finRootFolder, "data00", "mesta.dbf"),
            Path.Combine(_finRootFolder, "DATA00", "MESTA.DBF"),
            Path.Combine(_finRootFolder, "TXT", "MESTA.DBF")
        };

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate)) continue;
            if (string.Equals(candidate, targetDbfPath, StringComparison.OrdinalIgnoreCase)) continue;
            return candidate;
        }

        return null;
    }

    private static void KopirajMestaDatoteke(string sourceDbfPath, string targetDbfPath)
    {
        var sourceDir = Path.GetDirectoryName(sourceDbfPath) ?? string.Empty;
        var sourceBase = Path.GetFileNameWithoutExtension(sourceDbfPath);
        var targetDir = Path.GetDirectoryName(targetDbfPath) ?? string.Empty;
        var targetBase = Path.GetFileNameWithoutExtension(targetDbfPath);

        foreach (var ext in new[] { ".dbf", ".cdx", ".fpt", ".dbt" })
        {
            var sourceFile = Path.Combine(sourceDir, sourceBase + ext);
            if (!File.Exists(sourceFile)) continue;
            var targetFile = Path.Combine(targetDir, targetBase + ext);
            Directory.CreateDirectory(targetDir);
            File.Copy(sourceFile, targetFile, overwrite: true);
        }
    }

    private static void IsprazniDbfSadrzaj(string dbfPath)
    {
        using var fs = new FileStream(dbfPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Span<byte> header = stackalloc byte[32];
        fs.ReadExactly(header);

        var headerLength = header[8] | (header[9] << 8);
        if (headerLength < 33) throw new InvalidOperationException("Neispravan DBF header.");

        var now = DateTime.Now;
        header[1] = (byte)(now.Year - 1900);
        header[2] = (byte)now.Month;
        header[3] = (byte)now.Day;
        header[4] = 0; header[5] = 0; header[6] = 0; header[7] = 0;

        fs.Position = 0;
        fs.Write(header);
        fs.SetLength(headerLength + 1L);
        fs.Position = headerLength;
        fs.WriteByte(0x1A);
    }

    private static void ObrisiIndeksAkoPostoji(string dbfPath)
    {
        var cdxPath = Path.ChangeExtension(dbfPath, ".cdx");
        if (File.Exists(cdxPath)) File.Delete(cdxPath);
    }

    private static string? PronadjiMestaDbfUFolderu(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return null;
        return Directory.GetFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => string.Equals(Path.GetFileName(f), "mesta.dbf", StringComparison.OrdinalIgnoreCase));
    }

    private static string? PronadjiMestaDbf(string finRootFolder)
    {
        if (string.IsNullOrWhiteSpace(finRootFolder)) return null;

        var kandidati = new[]
        {
            Path.Combine(finRootFolder, "data00", "mesta.dbf"),
            Path.Combine(finRootFolder, "DATA00", "MESTA.DBF"),
            Path.Combine(finRootFolder, "mesta.dbf"),
        };

        foreach (var kandidat in kandidati)
            if (File.Exists(kandidat)) return kandidat;

        var data00 = Path.Combine(finRootFolder, "data00");
        if (Directory.Exists(data00))
        {
            var fromData00 = Directory.GetFiles(data00, "*.dbf", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => string.Equals(Path.GetFileName(f), "mesta.dbf", StringComparison.OrdinalIgnoreCase));
            if (fromData00 != null) return fromData00;
        }

        if (Directory.Exists(finRootFolder))
        {
            return Directory.GetFiles(finRootFolder, "*.dbf", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => string.Equals(Path.GetFileName(f), "mesta.dbf", StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }
}

public class GradoviStavka
{
    public string Mp { get; set; } = string.Empty;
    public string Posta { get; set; } = string.Empty;
    public string Mesto { get; set; } = string.Empty;
    public string Ziro1 { get; set; } = string.Empty;
    public string Ziro2 { get; set; } = string.Empty;
    public string PorBroj { get; set; } = string.Empty;
    public string PorBrojP { get; set; } = string.Empty;
    public string RegSoc { get; set; } = string.Empty;
    public decimal Por { get; set; }
    public decimal Zdr { get; set; }
    public decimal Pio { get; set; }
    public decimal Nez { get; set; }
    public string Vrsta { get; set; } = string.Empty;
    public string Preneto { get; set; } = string.Empty;
}
