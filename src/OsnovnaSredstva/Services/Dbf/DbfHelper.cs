using OsnovnaSredstva.Services;
using System.IO;

namespace OsnovnaSredstva.Services.Dbf;

internal static class DbfHelper
{
    internal static string? NadjiDbf(AppState appState, string ime)
    {
        var folderFirme = appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folderFirme)) return null;

        var hit = NadjiDbfUFolderu(folderFirme, ime);
        if (hit != null) return hit;

        var root = FinWorkspaceResolver.NormalizeRootPath(folderFirme);

        hit = NadjiDbfUFolderu(Path.Combine(root, "data00"), ime);
        if (hit != null) return hit;

        hit = NadjiDbfUFolderu(Path.Combine(folderFirme, "data00"), ime);
        if (hit != null) return hit;

        return NadjiDbfUFolderu(Path.Combine(AppContext.BaseDirectory, "data00"), ime);
    }

    internal static string? NadjiDbfGlobalno(string ime)
    {
        var hit = NadjiDbfUFolderu(Directory.GetCurrentDirectory(), ime);
        if (hit != null) return hit;

        hit = NadjiDbfUFolderu(Path.Combine(Directory.GetCurrentDirectory(), "data00"), ime);
        if (hit != null) return hit;

        hit = NadjiDbfUFolderu(AppContext.BaseDirectory, ime);
        if (hit != null) return hit;

        return NadjiDbfUFolderu(Path.Combine(AppContext.BaseDirectory, "data00"), ime);
    }

    internal static string? NadjiDbfUFolderu(string folder, string ime)
    {
        if (!Directory.Exists(folder)) return null;

        foreach (var naziv in new[] { ime, ime.ToUpperInvariant(), ime.ToLowerInvariant() })
        {
            var p = Path.Combine(folder, naziv);
            if (File.Exists(p)) return p;
        }

        return Directory.GetFiles(folder, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Equals(ime, StringComparison.OrdinalIgnoreCase));
    }

    internal static bool ImaZapisaDbf(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var header = new byte[12];
            if (fs.Read(header, 0, header.Length) < header.Length) return false;
            return BitConverter.ToInt32(header, 4) > 0;
        }
        catch { return false; }
    }
}
