using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace OsnovnaSredstva.ViewModels;

public partial class FormiranjeLozinkiViewModel : ObservableObject
{
    private readonly IPutanjaService _putanjaService;

    [ObservableProperty] private ObservableCollection<LozinkaStavka> _stavke = [];
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(ObrisiCommand))] private LozinkaStavka? _selektovana;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SacuvajCommand))] private string _id = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SacuvajCommand))] private string _korisnik = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SacuvajCommand))] private string _lozinka = string.Empty;
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SacuvajCommand))][NotifyCanExecuteChangedFor(nameof(ObrisiCommand))] private bool _ucitava;

    public FormiranjeLozinkiViewModel(IPutanjaService putanjaService)
    {
        _putanjaService = putanjaService;
        Osvezi();
    }

    partial void OnSelektovanaChanged(LozinkaStavka? value)
    {
        if (value is null) return;
        Id = value.Id;
        Korisnik = value.Korisnik;
        Lozinka = value.Lozinka;
    }

    private bool MozeSacuvaj() =>
        !Ucitava && !string.IsNullOrWhiteSpace(Id) &&
        !string.IsNullOrWhiteSpace(Korisnik) && !string.IsNullOrWhiteSpace(Lozinka);

    private bool MozeObrisi() => !Ucitava && Selektovana is not null;

    [RelayCommand]
    private void Osvezi()
    {
        Ucitava = true;
        try
        {
            if (!TryResolvePaths(out _, out var lozinkePath, out _, out var message))
            {
                Stavke = [];
                Poruka = message;
                return;
            }

            var rows = DbfReader.CitajSveZapise(lozinkePath);
            var lista = rows
                .Select(row => new LozinkaStavka
                {
                    Id = TwoDigit(DbfReader.Str(row, "PAS")),
                    Korisnik = DbfReader.Str(row, "KORISNIK"),
                    Lozinka = DbfReader.Str(row, "LOZINKA"),
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Korisnik))
                .OrderBy(item => ParseId(item.Id))
                .ThenBy(item => item.Korisnik, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Stavke = new ObservableCollection<LozinkaStavka>(lista);
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Učitano korisnika: {Stavke.Count}.";
        }
        catch (Exception ex)
        {
            Stavke = [];
            Poruka = $"Greška pri učitavanju: {ex.Message}";
        }
        finally { Ucitava = false; }
    }

    [RelayCommand]
    private void Novi()
    {
        Selektovana = null;
        Id = PredloziNaredniId();
        Korisnik = string.Empty;
        Lozinka = string.Empty;
        Poruka = "Unesite ID, korisničko ime i lozinku pa kliknite Sačuvaj.";
    }

    [RelayCommand(CanExecute = nameof(MozeSacuvaj))]
    private void Sacuvaj()
    {
        Ucitava = true;
        try
        {
            if (!TryParseOperatorId(Id, out var operatorId))
            {
                Poruka = "ID mora biti broj od 1 do 99.";
                return;
            }

            if (!TryResolvePaths(out var rootPath, out var lozinkePath, out var lozinkeaPath, out var message))
            {
                Poruka = message;
                return;
            }

            var korisnikIme = Korisnik.Trim();
            var lozinkaTekst = Lozinka.Trim();
            var pas = operatorId.ToString("00", CultureInfo.InvariantCulture);

            var schema = DbfTableWriter.LoadSchema(lozinkePath);
            var rows = DbfReader.CitajSveZapise(lozinkePath)
                .Select(r => new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var duplikat = rows.FirstOrDefault(row =>
                string.Equals(DbfReader.Str(row, "KORISNIK"), korisnikIme, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(TwoDigit(DbfReader.Str(row, "PAS")), pas, StringComparison.OrdinalIgnoreCase));

            if (duplikat is not null)
            {
                Poruka = $"Korisnik '{korisnikIme}' već postoji pod ID {TwoDigit(DbfReader.Str(duplikat, "PAS"))}.";
                return;
            }

            var target = rows.FirstOrDefault(row =>
                string.Equals(TwoDigit(DbfReader.Str(row, "PAS")), pas, StringComparison.OrdinalIgnoreCase));

            if (target is null)
            {
                target = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                rows.Add(target);
            }

            target["PAS"] = pas;
            target["KORISNIK"] = korisnikIme;
            target["LOZINKA"] = lozinkaTekst;
            target["KORIME"] = korisnikIme;
            target["AKTIVAN"] = "D";
            target["OPERATER"] = string.Equals(pas, "01", StringComparison.OrdinalIgnoreCase) ? "1" : DbfReader.Str(target, "OPERATER");
            target["PASSNIVO"] = DbfReader.Dec(target, "PASSNIVO") is 0m ? 1m : DbfReader.Dec(target, "PASSNIVO");
            target["DATUM"] = DateTime.Today;
            target["PRENETO"] = " ";
            target["IDBR"] = DbfReader.Dec(target, "IDBR") is 0m ? NextIdbr(rows) : DbfReader.Dec(target, "IDBR");

            SetIfMissing(target, "PASSGK", true); SetIfMissing(target, "PASSAN", true);
            SetIfMissing(target, "PASSBL", true); SetIfMissing(target, "PASSTV", true);
            SetIfMissing(target, "PASSTM", true); SetIfMissing(target, "PASSUS", true);
            SetIfMissing(target, "PASSLD", true); SetIfMissing(target, "PASSOST", true);
            SetIfMissing(target, "PASSPRN", true); SetIfMissing(target, "PASSPRO", true);
            SetIfMissing(target, "PASSOS", true); SetIfMissing(target, "PASSPROF", true);
            SetIfMissing(target, "PASSDEL", true);

            DbfTableWriter.WriteTable(lozinkePath, schema, rows,
                static (row, fieldName) => row.TryGetValue(fieldName, out var value) ? value : null);

            UpsertLozinkea(lozinkeaPath, pas, korisnikIme, DbfReader.Dec(target, "IDBR"));
            FinWorkspaceResolver.EnsureDataFoldersForOperator(rootPath, operatorId);

            Poruka = $"Sačuvan korisnik '{korisnikIme}' (ID {pas}).";
            Osvezi();
            Selektovana = Stavke.FirstOrDefault(item => item.Id == pas);
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čuvanju: {ex.Message}";
        }
        finally { Ucitava = false; }
    }

    [RelayCommand(CanExecute = nameof(MozeObrisi))]
    private void Obrisi()
    {
        if (Selektovana is null) return;
        Ucitava = true;
        try
        {
            if (!TryResolvePaths(out _, out var lozinkePath, out var lozinkeaPath, out var message))
            {
                Poruka = message;
                return;
            }

            var pas = TwoDigit(Selektovana.Id);
            var schema = DbfTableWriter.LoadSchema(lozinkePath);
            var rows = DbfReader.CitajSveZapise(lozinkePath)
                .Select(r => new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase))
                .Where(row => !string.Equals(TwoDigit(DbfReader.Str(row, "PAS")), pas, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (rows.Count == 0)
            {
                Poruka = "Mora ostati bar jedan korisnik u tabeli.";
                return;
            }

            DbfTableWriter.WriteTable(lozinkePath, schema, rows,
                static (row, fieldName) => row.TryGetValue(fieldName, out var value) ? value : null);

            RemoveLozinkea(lozinkeaPath, pas);
            Poruka = $"Obrisan je korisnik ID {pas}.";
            Osvezi();
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri brisanju: {ex.Message}";
        }
        finally { Ucitava = false; }
    }

    private bool TryResolvePaths(out string rootPath, out string lozinkePath, out string lozinkeaPath, out string message)
    {
        rootPath = lozinkePath = lozinkeaPath = message = string.Empty;
        var finPutanja = _putanjaService.DajFinPutanju();
        if (string.IsNullOrWhiteSpace(finPutanja))
        {
            message = "Putanja nije postavljena.";
            return false;
        }

        rootPath = FinWorkspaceResolver.NormalizeRootPath(finPutanja);
        if (!Directory.Exists(rootPath))
        {
            message = $"Putanja ne postoji: {rootPath}";
            return false;
        }

        return FinWorkspaceResolver.EnsureLozinkeTables(rootPath, out lozinkePath, out lozinkeaPath, out message);
    }

    private static void UpsertLozinkea(string lozinkeaPath, string pas, string korisnik, decimal idbr)
    {
        var schema = DbfTableWriter.LoadSchema(lozinkeaPath);
        var rows = DbfReader.CitajSveZapise(lozinkeaPath)
            .Select(r => new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var target = rows.FirstOrDefault(row =>
            string.Equals(TwoDigit(DbfReader.Str(row, "PAS")), pas, StringComparison.OrdinalIgnoreCase));

        if (target is null) { target = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase); rows.Add(target); }

        target["PAS"] = pas;
        target["KORISNIK"] = korisnik;
        target["AKTIVAN"] = "D";
        target["DATUM"] = DateTime.Today;
        target["PRENETO"] = " ";
        target["IDBR"] = idbr > 0m ? idbr : NextIdbr(rows);

        DbfTableWriter.WriteTable(lozinkeaPath, schema, rows,
            static (row, fieldName) => row.TryGetValue(fieldName, out var value) ? value : null);
    }

    private static void RemoveLozinkea(string lozinkeaPath, string pas)
    {
        var schema = DbfTableWriter.LoadSchema(lozinkeaPath);
        var rows = DbfReader.CitajSveZapise(lozinkeaPath)
            .Select(r => new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase))
            .Where(row => !string.Equals(TwoDigit(DbfReader.Str(row, "PAS")), pas, StringComparison.OrdinalIgnoreCase))
            .ToList();

        DbfTableWriter.WriteTable(lozinkeaPath, schema, rows,
            static (row, fieldName) => row.TryGetValue(fieldName, out var value) ? value : null);
    }

    private string PredloziNaredniId()
    {
        var max = Stavke.Select(item => ParseId(item.Id)).DefaultIfEmpty(0).Max();
        return Math.Clamp(max + 1, 1, 99).ToString("00", CultureInfo.InvariantCulture);
    }

    private static bool TryParseOperatorId(string value, out int operatorId)
    {
        operatorId = 0;
        if (!int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return false;
        if (parsed is < 1 or > 99) return false;
        operatorId = parsed;
        return true;
    }

    private static int ParseId(string value)
        => int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0;

    private static string TwoDigit(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id.ToString("00", CultureInfo.InvariantCulture) : trimmed;
    }

    private static decimal NextIdbr(IReadOnlyList<Dictionary<string, object?>> rows)
    {
        var max = rows.Select(row => DbfReader.Dec(row, "IDBR")).DefaultIfEmpty(0m).Max();
        return max + 1m;
    }

    private static void SetIfMissing(Dictionary<string, object?> row, string key, object value)
    {
        if (!row.ContainsKey(key)) row[key] = value;
    }
}

public partial class LozinkaStavka : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _korisnik = string.Empty;
    [ObservableProperty] private string _lozinka = string.Empty;
}
