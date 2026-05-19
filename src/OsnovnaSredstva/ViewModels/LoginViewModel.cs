using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using System.IO;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private const string LokalniModMarker = ".algoritam-local-no-password";

    private readonly IAuthService _authService;
    private readonly ILoginSessionService _loginSessionService;
    private readonly IPutanjaService _putanjaService;
    private readonly AppState _appState;
    private readonly bool _lokalniModBezLozinke;

    public event Action? PrijavaUspela;

    public LoginViewModel(
        IAuthService authService,
        ILoginSessionService loginSessionService,
        IPutanjaService putanjaService,
        AppState appState)
    {
        _authService = authService;
        _loginSessionService = loginSessionService;
        _putanjaService = putanjaService;
        _appState = appState;

        _lokalniModBezLozinke = JeLokalniMod();
        if (_lokalniModBezLozinke)
            KorisnikIme = "admin";
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrijavaCommand))]
    private string _korisnikIme = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrijavaCommand))]
    private string _lozinka = string.Empty;

    [ObservableProperty]
    private string _poruka = string.Empty;

    [ObservableProperty]
    private bool _ucitava;

    public bool LozinkaNijeObavezna => _lokalniModBezLozinke;

    private bool MozePrijava() =>
        !string.IsNullOrWhiteSpace(KorisnikIme) &&
        (_lokalniModBezLozinke || !string.IsNullOrWhiteSpace(Lozinka)) &&
        !Ucitava;

    [RelayCommand(CanExecute = nameof(MozePrijava))]
    private async Task PrijavaAsync()
    {
        Ucitava = true;
        Poruka = string.Empty;

        try
        {
            if (_lokalniModBezLozinke && string.IsNullOrWhiteSpace(Lozinka))
            {
                _appState.Prijavi(KreirajLokalnogKorisnika(KorisnikIme.Trim()));
                PrijavaUspela?.Invoke();
                return;
            }

            var rezultat = await _authService.PrijavaAsync(KorisnikIme.Trim(), Lozinka);

            if (rezultat.Uspelo && rezultat.Korisnik != null)
            {
                if (!_loginSessionService.TryAcquireSession(rezultat.Korisnik.KorisnikIme, out var lockHandle, out var lockMsg))
                {
                    Poruka = string.IsNullOrWhiteSpace(lockMsg)
                        ? $"Korisnik '{rezultat.Korisnik.KorisnikIme}' je već prijavljen."
                        : lockMsg;
                    Lozinka = string.Empty;
                    return;
                }

                _appState.PostaviSesijaLock(lockHandle);
                _appState.Prijavi(rezultat.Korisnik);
                PrijavaUspela?.Invoke();
            }
            else
            {
                Poruka = string.IsNullOrWhiteSpace(rezultat.Poruka)
                    ? "Pogrešan korisnik ili lozinka."
                    : rezultat.Poruka;
                Lozinka = string.Empty;
            }
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand]
    private void PrikaziDijagnostiku()
    {
        try
        {
            var finPutanja = _putanjaService.DajFinPutanju();
            if (string.IsNullOrEmpty(finPutanja))
            {
                MessageBox.Show("FIN putanja nije postavljena.", "Dijagnostika",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var lozinkeFajl = DbfDijagnostika.PronadjiLozinkeFajl(finPutanja);
            if (lozinkeFajl is null)
            {
                var data00 = Path.Combine(finPutanja, "data00");
                var info = Directory.Exists(data00)
                    ? string.Join("\n", Directory.GetFiles(data00, "*.DBF")
                        .Select(Path.GetFileName))
                    : $"Folder data00 ne postoji u: {finPutanja}";

                MessageBox.Show(
                    $"LOZINKE.DBF nije pronađen.\n\nRoot: {finPutanja}\n\nDBF fajlovi u data00:\n{info}",
                    "Dijagnostika", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var izvestaj = DbfDijagnostika.AnalizirajFajl(lozinkeFajl);
            MessageBox.Show(izvestaj, $"Dijagnostika — {Path.GetFileName(lozinkeFajl)}",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška:\n{ex.Message}", "Dijagnostika",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool JeLokalniMod()
    {
        var finPutanja = _putanjaService.DajFinPutanju();
        if (string.IsNullOrWhiteSpace(finPutanja)) return false;

        var marker = Path.Combine(finPutanja, LokalniModMarker);
        if (!File.Exists(marker)) return false;

        return DbfDijagnostika.PronadjiLozinkeFajl(finPutanja) is null;
    }

    private static Korisnik KreirajLokalnogKorisnika(string ime) => new()
    {
        Pas = "0",
        KorisnikIme = string.IsNullOrWhiteSpace(ime) ? "admin" : ime,
        KorisnikIme2 = "Lokalni korisnik",
        Lozinka = string.Empty,
        Aktivan = true,
        JeSupervizor = true,
        PravaNivo = 1,
        PassGk = true, PassAn = true, PassBl = true, PassTv = true,
        PassTm = true, PassUs = true, PassLd = true, PassOst = true,
        PassPrn = true, PassPro = true, PassOs = true, PassProf = true,
        PassDel = true
    };
}
