using System.Globalization;
using System.IO;
using System.Text;

namespace OsnovnaSredstva.Services.Dbf;

public static class DbfOptimisticConcurrency
{
    public sealed record FileSnapshot(DateTime LastWriteUtc, long Length);

    public static FileSnapshot CaptureFileSnapshot(string path)
    {
        var info = new FileInfo(path);
        return new FileSnapshot(info.LastWriteTimeUtc, info.Exists ? info.Length : 0L);
    }

    public static bool HasFileChanged(string path, FileSnapshot snapshot)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
            return snapshot.Length != 0L || snapshot.LastWriteUtc != DateTime.MinValue;
        return info.LastWriteTimeUtc != snapshot.LastWriteUtc || info.Length != snapshot.Length;
    }
}
