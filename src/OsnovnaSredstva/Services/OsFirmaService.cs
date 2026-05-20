using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services.Dbf;
using System.IO;
using System.Text.RegularExpressions;

namespace OsnovnaSredstva.Services;

public class OsFirmaService : IFirmaService
{
    private static readonly Regex FolderRegex =
        new(@"^F\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IPutanjaService _putanjaService;

    public OsFirmaService(IPutanjaService putanjaService)
    {
        _putanjaService = putanjaService;
    }

    public Task<List<Firma>> DajSveFirmeAsync()
    {
        return Task.Run(DajSveFirme);
    }

    private List<Firma> DajSveFirme()
    {
        var finPutanja = _putanjaService.DajFinPutanju();
        if (string.IsNullOrWhiteSpace(finPutanja)) return [];

        var firme = new List<Firma>();
        var folderiFirmi = Directory.GetDirectories(finPutanja)
            .Where(d => FolderRegex.IsMatch(Path.GetFileName(d)))
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var folder in folderiFirmi)
        {
            var firma = CitajFirmu(folder);
            firme.Add(firma);
        }

        return firme;
    }

    private static Firma CitajFirmu(string folderPath)
    {
        var folderIme = Path.GetFileName(folderPath);
        var firma = new Firma
        {
            FolderPath = folderPath,
            FolderIme = folderIme,
            Naziv = folderIme,
            Aktivna = false
        };

        var firmFajl = PronadjiDbfFajl(folderPath, "FIRMA");
        if (firmFajl is null) return firma;

        try
        {
            var reader = new SimpleDbfReader(firmFajl);
            var prvizapis = reader.Zapisi().FirstOrDefault();
            if (prvizapis is null) return firma;

            // Pokušavamo različita moguća imena polja
            // Podrzimo i "stara" i "nova" imena polja da bi izbor firme
            // prikazivao iste podatke kao ekran "Podaci o firmi".
            firma.Naziv = NeprazanString(
                prvizapis.DajString("FIME"),
                prvizapis.DajString("NAZIV_F"),
                prvizapis.DajString("NAZIV"),
                prvizapis.DajString("NAZ_FIRM"),
                folderIme);

            firma.Naziv2 = prvizapis.DajString("FIME2").Trim();

            firma.Baza = NeprazanString(
                prvizapis.DajString("FBAZA"),
                prvizapis.DajString("BAZA"),
                prvizapis.DajString("SIFRA"));

            firma.Maticni = NeprazanString(
                prvizapis.DajString("FMAT"),
                prvizapis.DajString("MAT_BR"),
                prvizapis.DajString("MATICNI"));
            firma.MatBr = firma.Maticni;

            firma.Pib = NeprazanString(
                prvizapis.DajString("FPOR"),
                prvizapis.DajString("PIB"),
                prvizapis.DajString("JMB"));

            firma.Ulica         = prvizapis.DajString("FUL").Trim();
            firma.BrojUlice     = prvizapis.DajString("FULBR").Trim();
            firma.PostanskiBroj = prvizapis.DajString("FPOS").Trim();
            firma.Mesto         = prvizapis.DajString("FMES").Trim();
            firma.ZiroRacun     = prvizapis.DajString("FZIRO").Trim();
            firma.Aktivna = true;
        }
        catch { }

        return firma;
    }

    private static string NeprazanString(params string[] vrednosti)
    {
        foreach (var v in vrednosti)
            if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
        return string.Empty;
    }

    private static string? PronadjiDbfFajl(string folder, string baseName)
    {
        foreach (var ext in new[] { ".DBF", ".dbf" })
        {
            var putanja = Path.Combine(folder, baseName + ext);
            if (File.Exists(putanja)) return putanja;
        }
        return null;
    }

    public Task<Firma?> DodajFirmuAsync()
    {
        return Task.Run(DodajFirmu);
    }

    private Firma? DodajFirmu()
    {
        var finPutanja = _putanjaService.DajFinPutanju();
        if (string.IsNullOrWhiteSpace(finPutanja)) return null;

        // Pronađi sledeći slobodan F broj
        var postojeci = Directory.GetDirectories(finPutanja)
            .Select(Path.GetFileName)
            .Where(n => FolderRegex.IsMatch(n!))
            .Select(n => int.TryParse(n![1..], out var num) ? num : 0)
            .Where(n => n > 0)
            .ToHashSet();

        int sledeci = 1;
        while (postojeci.Contains(sledeci)) sledeci++;

        var noviFolder = Path.Combine(finPutanja, $"F{sledeci}");
        Directory.CreateDirectory(noviFolder);

        // Kopiraj template iz data01 ako postoji
        var templateData01 = Path.Combine(finPutanja, "data01");
        if (Directory.Exists(templateData01))
            KopirajFolder(templateData01, noviFolder);

        // Kopiraj OS DBF template (OSIZFINA) ako novi folder nema os DBF fajlove
        KopirajOsTemplate(noviFolder);

        return new Firma
        {
            FolderPath = noviFolder,
            FolderIme = $"F{sledeci}",
            Naziv = $"F{sledeci}",
            Aktivna = true
        };
    }

    public Task<bool> ObrisiFirmuAsync(string folderPath)
    {
        return Task.Run(() =>
        {
            try
            {
                if (Directory.Exists(folderPath))
                    Directory.Delete(folderPath, recursive: true);
                return true;
            }
            catch { return false; }
        });
    }

    private static void KopirajFolder(string izvor, string cilj)
    {
        Directory.CreateDirectory(cilj);
        foreach (var fajl in Directory.GetFiles(izvor))
            File.Copy(fajl, Path.Combine(cilj, Path.GetFileName(fajl)), overwrite: true);
        foreach (var sub in Directory.GetDirectories(izvor))
            KopirajFolder(sub, Path.Combine(cilj, Path.GetFileName(sub)));
    }

    private static void KopirajOsTemplate(string cilj)
    {
        // Ako folder već ima OS fajlove, ne prepisuj
        if (Directory.GetFiles(cilj, "os*.dbf", SearchOption.TopDirectoryOnly).Length > 0)
            return;
        if (Directory.GetFiles(cilj, "OS*.DBF", SearchOption.TopDirectoryOnly).Length > 0)
            return;

        // Traži OSIZFINA template pored exe-a ili u src projektu
        var kandidati = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "OSIZFINA"),
            Path.Combine(AppContext.BaseDirectory, "osizfina"),
        };

        foreach (var template in kandidati)
        {
            if (!Directory.Exists(template)) continue;
            foreach (var fajl in Directory.GetFiles(template))
                File.Copy(fajl, Path.Combine(cilj, Path.GetFileName(fajl)), overwrite: false);
            return;
        }
    }
}
