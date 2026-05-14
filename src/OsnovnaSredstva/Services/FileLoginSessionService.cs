using System.IO;

namespace OsnovnaSredstva.Services;

public class FileLoginSessionService : ILoginSessionService
{
    private readonly IPutanjaService _putanjaService;

    public FileLoginSessionService(IPutanjaService putanjaService)
    {
        _putanjaService = putanjaService;
    }

    public bool TryAcquireSession(string korisnikIme, out IDisposable? lockHandle, out string? poruka)
    {
        poruka = null;
        try
        {
            var putanja = _putanjaService.DajFinPutanju();
            if (string.IsNullOrWhiteSpace(putanja))
            {
                lockHandle = new NoopDisposable();
                return true;
            }

            var lockFile = Path.Combine(putanja, $".session_{korisnikIme.ToLowerInvariant()}.lock");
            var stream = new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            lockHandle = new FileLockHandle(stream, lockFile);
            return true;
        }
        catch (IOException)
        {
            poruka = $"Korisnik '{korisnikIme}' je već prijavljen u drugoj instanci programa.";
            lockHandle = null;
            return false;
        }
        catch
        {
            lockHandle = new NoopDisposable();
            return true;
        }
    }

    public void ReleaseSession(string korisnikIme)
    {
        try
        {
            var putanja = _putanjaService.DajFinPutanju();
            if (string.IsNullOrWhiteSpace(putanja)) return;
            var lockFile = Path.Combine(putanja, $".session_{korisnikIme.ToLowerInvariant()}.lock");
            if (File.Exists(lockFile)) File.Delete(lockFile);
        }
        catch { }
    }

    private sealed class FileLockHandle : IDisposable
    {
        private readonly FileStream _stream;
        private readonly string _path;
        private bool _disposed;

        public FileLockHandle(FileStream stream, string path)
        {
            _stream = stream;
            _path = path;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _stream.Dispose(); } catch { }
            try { File.Delete(_path); } catch { }
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
