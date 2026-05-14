using OsnovnaSredstva.Models;

namespace OsnovnaSredstva.Services;

public interface IAuthService
{
    Task<Korisnik?> PrijavaAsync(string korisnikIme, string lozinka);
}
