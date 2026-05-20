using OsnovnaSredstva.Services.Dbf;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace OsnovnaSredstva.Services;

public static class FinWorkspaceResolver
{
    private static readonly Regex FirmaFolderRegex =
        new("^F\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DataFolderRegex =
        new("^data\\d{2}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] FinMarkerFolders = ["data00", "databaze", "00FIN"];
    private static readonly string[] SidecarExtensions = [".dbf", ".cdx", ".fpt", ".dbt"];

    private static readonly DbfFieldDef[] LozinkeMinimalFields =
    [
        new("PAS", 'C', 2),
        new("KORISNIK", 'C', 20),
        new("LOZINKA", 'C', 20),
        new("KORIME", 'C', 30),
        new("AKTIVAN", 'C', 1),
        new("OPERATER", 'C', 1),
        new("PASSNIVO", 'N', 1, 0),
        new("PASSGK", 'L', 1),
        new("PASSAN", 'L', 1),
        new("PASSBL", 'L', 1),
        new("PASSTV", 'L', 1),
        new("PASSTM", 'L', 1),
        new("PASSUS", 'L', 1),
        new("PASSLD", 'L', 1),
        new("PASSOST", 'L', 1),
        new("PASSPRN", 'L', 1),
        new("PASSPRO", 'L', 1),
        new("PASSOS", 'L', 1),
        new("PASSPROF", 'L', 1),
        new("PASSDEL", 'L', 1),
        new("DATUM", 'D', 8),
        new("PRENETO", 'C', 1),
        new("IDBR", 'N', 11, 0),
    ];

    private static readonly DbfFieldDef[] LozinkeaMinimalFields =
    [
        new("PAS", 'C', 2),
        new("KORISNIK", 'C', 20),
        new("AKTIVAN", 'C', 1),
        new("DATUM", 'D', 8),
        new("PRENETO", 'C', 1),
        new("IDBR", 'N', 11, 0),
    ];

    public static string NormalizeRootPath(string path)
    {
        var trimmed = (path ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return string.Empty;

        var full = Path.GetFullPath(trimmed);
        if (!Directory.Exists(full)) return full;

        var name = Path.GetFileName(full);
        var parent = Directory.GetParent(full)?.FullName;
        if (string.IsNullOrWhiteSpace(parent)) return full;

        if (FirmaFolderRegex.IsMatch(name) || DataFolderRegex.IsMatch(name))
            return parent;

        return full;
    }

    public static bool IsClassicFinRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var root = NormalizeRootPath(path!);
        return FinMarkerFolders.Any(marker => Directory.Exists(Path.Combine(root, marker)));
    }

    public static string GetData00Path(string rootPath)
        => Path.Combine(NormalizeRootPath(rootPath), "data00");

    public static bool EnsureLozinkeTables(
        string rootPath,
        out string lozinkePath,
        out string lozinkeaPath,
        out string message)
    {
        lozinkePath = string.Empty;
        lozinkeaPath = string.Empty;
        message = string.Empty;

        var root = NormalizeRootPath(rootPath);
        if (string.IsNullOrWhiteSpace(root))
        {
            message = "Putanja nije validna.";
            return false;
        }

        try
        {
            Directory.CreateDirectory(root);
            var data00 = GetData00Path(root);
            Directory.CreateDirectory(data00);

            lozinkePath = Path.Combine(data00, "LOZINKE.DBF");
            lozinkeaPath = Path.Combine(data00, "LOZINKEA.DBF");

            EnsureTableExists(root, lozinkePath, "lozinke", LozinkeMinimalFields);
            EnsureTableExists(root, lozinkeaPath, "lozinkea", LozinkeaMinimalFields);

            EnsureDefaultAdminUser(lozinkePath);

            return true;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }
    }

    public static string? ResolveLozinkeTablePath(string? rootPath)
        => ResolveTablePath(rootPath, "lozinke");

    public static void EnsureDataFoldersForOperator(string rootPath, int operatorId)
    {
        if (operatorId < 1 || operatorId > 99) return;

        var root = NormalizeRootPath(rootPath);
        if (string.IsNullOrWhiteSpace(root)) return;

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "data01"));

        var suffix = operatorId.ToString("00", CultureInfo.InvariantCulture);
        var dataTarget = Path.Combine(root, $"data{suffix}");

        var dataTemplate = Directory.Exists(Path.Combine(root, "data01"))
            ? Path.Combine(root, "data01") : null;

        EnsureFolderFromTemplate(dataTarget, dataTemplate);
    }

    private static string? ResolveTablePath(string? rootPath, string tableBaseName)
    {
        if (string.IsNullOrWhiteSpace(rootPath)) return null;

        var root = NormalizeRootPath(rootPath!);
        var tableFileName = $"{tableBaseName}.dbf";

        var data00 = Path.Combine(root, "data00");
        var found = FindCaseInsensitiveFile(data00, tableFileName);
        if (!string.IsNullOrWhiteSpace(found)) return found;

        var databaze = Path.Combine(root, "databaze");
        found = FindCaseInsensitiveFile(databaze, tableFileName);
        if (!string.IsNullOrWhiteSpace(found)) return found;

        found = FindCaseInsensitiveFile(root, tableFileName);
        if (!string.IsNullOrWhiteSpace(found)) return found;

        foreach (var firma in Directory.GetDirectories(root, "F*", SearchOption.TopDirectoryOnly))
        {
            if (!FirmaFolderRegex.IsMatch(Path.GetFileName(firma))) continue;

            found = FindCaseInsensitiveFile(firma, tableFileName);
            if (!string.IsNullOrWhiteSpace(found)) return found;
        }

        return null;
    }

    private static void EnsureTableExists(
        string rootPath, string targetDbfPath, string tableBaseName,
        IReadOnlyList<DbfFieldDef> fallbackFields)
    {
        if (File.Exists(targetDbfPath)) return;

        var existingPath = ResolveTablePath(rootPath, tableBaseName);
        if (!string.IsNullOrWhiteSpace(existingPath))
        {
            CopyDbfFamily(existingPath, targetDbfPath);
            return;
        }

        CreateMinimalDbf(targetDbfPath, fallbackFields);
    }

    private static void EnsureDefaultAdminUser(string lozinkePath)
    {
        var existingRows = SafeReadRows(lozinkePath);
        if (existingRows.Count > 0) return;

        var schema = DbfTableWriter.LoadSchema(lozinkePath);
        var admin = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["PAS"] = "01",
            ["KORISNIK"] = "admin",
            ["LOZINKA"] = "admin",
            ["KORIME"] = "Administrator",
            ["AKTIVAN"] = "D",
            ["OPERATER"] = "1",
            ["PASSNIVO"] = 1m,
            ["PASSGK"] = true, ["PASSAN"] = true, ["PASSBL"] = true, ["PASSTV"] = true,
            ["PASSTM"] = true, ["PASSUS"] = true, ["PASSLD"] = true, ["PASSOST"] = true,
            ["PASSPRN"] = true, ["PASSPRO"] = true, ["PASSOS"] = true, ["PASSPROF"] = true,
            ["PASSDEL"] = true,
            ["DATUM"] = DateTime.Today,
            ["PRENETO"] = " ",
            ["IDBR"] = 1m,
        };

        DbfTableWriter.WriteTable(
            lozinkePath, schema, [admin],
            static (row, fieldName) => row.TryGetValue(fieldName, out var value) ? value : null);
    }

    private static List<Dictionary<string, object?>> SafeReadRows(string dbfPath)
    {
        try
        {
            return DbfReader.CitajSveZapise(dbfPath)
                .Select(row => new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
        catch { return []; }
    }

    private static void EnsureFolderFromTemplate(string targetFolder, string? templateFolder)
    {
        if (Directory.Exists(targetFolder)) return;
        Directory.CreateDirectory(targetFolder);

        if (string.IsNullOrWhiteSpace(templateFolder) || !Directory.Exists(templateFolder)) return;

        foreach (var filePath in Directory.GetFiles(templateFolder, "*", SearchOption.TopDirectoryOnly))
            File.Copy(filePath, Path.Combine(targetFolder, Path.GetFileName(filePath)), overwrite: true);
    }

    private static void CopyDbfFamily(string sourceDbfPath, string targetDbfPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetDbfPath)!);
        var sourceBase = Path.Combine(Path.GetDirectoryName(sourceDbfPath)!, Path.GetFileNameWithoutExtension(sourceDbfPath));
        var targetBase = Path.Combine(Path.GetDirectoryName(targetDbfPath)!, Path.GetFileNameWithoutExtension(targetDbfPath));

        foreach (var ext in SidecarExtensions)
        {
            var sourceFile = sourceBase + ext;
            if (!File.Exists(sourceFile)) continue;
            File.Copy(sourceFile, targetBase + ext, overwrite: true);
        }
    }

    private static string? FindCaseInsensitiveFile(string? directory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return null;

        return Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(path =>
                string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase));
    }

    private static void CreateMinimalDbf(string targetPath, IReadOnlyList<DbfFieldDef> fields)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        var recordLength = 1 + fields.Sum(field => field.Length);
        var headerLength = 32 + fields.Count * 32 + 1;

        using var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        var now = DateTime.Now;
        writer.Write((byte)0x03);
        writer.Write((byte)(now.Year - 1900));
        writer.Write((byte)now.Month);
        writer.Write((byte)now.Day);
        writer.Write((uint)0);
        writer.Write((ushort)headerLength);
        writer.Write((ushort)recordLength);
        writer.Write(new byte[16]);
        writer.Write((byte)0x00);
        writer.Write((byte)0xC8); // CP1250
        writer.Write((ushort)0x0000);

        foreach (var field in fields)
        {
            var descriptor = new byte[32];
            var nameBytes = Encoding.ASCII.GetBytes(field.Name.Trim().ToUpperInvariant());
            Array.Copy(nameBytes, descriptor, Math.Min(nameBytes.Length, 11));
            descriptor[11] = (byte)field.Type;
            descriptor[16] = field.Length;
            descriptor[17] = field.Decimals;
            writer.Write(descriptor);
        }

        writer.Write((byte)0x0D);
        writer.Write((byte)0x1A);
    }

    private static readonly DbfFieldDef[] FirmaMinimalFields =
    [
        new("FIME",     'C', 60),
        new("FIME2",    'C', 60),
        new("FIMEC",    'C', 60),
        new("FBAZA",    'C', 10),
        new("FVLAST",   'C', 40),
        new("FOSOBA",   'C', 40),
        new("FOBLIK",   'C', 40),
        new("FMAT",     'C', 20),
        new("FPOR",     'C', 20),
        new("FPDV",     'C',  1),
        new("FSIF",     'C', 10),
        new("FNAZD",    'C', 60),
        new("FPOS",     'C', 10),
        new("FMES",     'C', 40),
        new("FUL",      'C', 40),
        new("FULBR",    'C', 10),
        new("FOPS",     'C', 40),
        new("FREPUB",   'C', 40),
        new("FDRZAVA",  'C', 40),
        new("FTEL",     'C', 20),
        new("FTEL2",    'C', 20),
        new("FFAX",     'C', 20),
        new("FEMAIL",   'C', 60),
        new("FVEB",     'C', 60),
        new("FAGENC",   'C', 40),
        new("FZIRO",    'C', 40),
        new("FZIRO2",   'C', 40),
        new("FZIRODEV", 'C', 40),
        new("FZIROBOL", 'C', 40),
        new("FBANKA",   'C', 40),
        new("FBANKA2",  'C', 40),
        new("FBANKAD",  'C', 40),
        new("FBANKAB",  'C', 40),
        new("FSWIFT",   'C', 20),
        new("FDAT0",    'D',  8),
        new("FDATREG",  'D',  8),
        new("FDATUPIS", 'D',  8),
        new("FDATPDV",  'D',  8),
        new("FREGSOC",  'C', 20),
        new("FREGZDR",  'C', 20),
        new("FREGSUD",  'C', 40),
    ];

    public static string EnsureFirmaDbf(string folderPath)
    {
        var putanja = Path.Combine(folderPath, "firma.dbf");
        if (!File.Exists(putanja))
        {
            Directory.CreateDirectory(folderPath);
            CreateMinimalDbf(putanja, FirmaMinimalFields);
        }
        return putanja;
    }

    private readonly record struct DbfFieldDef(string Name, char Type, byte Length, byte Decimals = 0);
}
