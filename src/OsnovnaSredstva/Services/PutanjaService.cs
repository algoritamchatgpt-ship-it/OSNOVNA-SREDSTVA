using System.IO;
using System.Text.Json;

namespace OsnovnaSredstva.Services;

public class PutanjaService : IPutanjaService
{
    private const string AppFolderIme = "OsnovnaSredstva";
    private const string PodaciIme = "putanje.json";

    private readonly string _podaciPutanja;
    private PutanjeModel _podaci;

    public PutanjaService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, AppFolderIme);
        Directory.CreateDirectory(folder);
        _podaciPutanja = Path.Combine(folder, PodaciIme);
        _podaci = UcitajPodatke();
    }

    public string? DajFinPutanju() =>
        string.IsNullOrWhiteSpace(_podaci.FinPutanja) ? null : _podaci.FinPutanja;

    public bool SnimiFinPutanju(string putanja)
    {
        _podaci.FinPutanja = putanja;
        return SnimiPodatke();
    }

    public bool JeValidanFinFolder(string putanja)
    {
        if (string.IsNullOrWhiteSpace(putanja) || !Directory.Exists(putanja))
            return false;

        // Mora imati bar jedan F-folder (F1, F2 ...) ili data00
        var imaFolderFirme = Directory.GetDirectories(putanja)
            .Any(d => System.Text.RegularExpressions.Regex.IsMatch(
                Path.GetFileName(d), @"^F\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase));

        var imaData00 = Directory.Exists(Path.Combine(putanja, "data00"));

        return imaFolderFirme || imaData00;
    }

    public string? DajArhivaPutanju() =>
        string.IsNullOrWhiteSpace(_podaci.ArhivaPutanja) ? null : _podaci.ArhivaPutanja;

    public bool SnimiArhivaPutanju(string putanja)
    {
        _podaci.ArhivaPutanja = putanja;
        return SnimiPodatke();
    }

    private PutanjeModel UcitajPodatke()
    {
        try
        {
            if (!File.Exists(_podaciPutanja)) return new();
            var json = File.ReadAllText(_podaciPutanja);
            return JsonSerializer.Deserialize<PutanjeModel>(json) ?? new();
        }
        catch { return new(); }
    }

    private bool SnimiPodatke()
    {
        try
        {
            var json = JsonSerializer.Serialize(_podaci, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_podaciPutanja, json);
            return true;
        }
        catch { return false; }
    }

    public string? DajIzvozPutanju() =>
        string.IsNullOrWhiteSpace(_podaci.IzvozPutanja) ? null : _podaci.IzvozPutanja;

    public bool SnimiIzvozPutanju(string putanja)
    {
        _podaci.IzvozPutanja = putanja;
        return SnimiPodatke();
    }

    private class PutanjeModel
    {
        public string FinPutanja { get; set; } = string.Empty;
        public string ArhivaPutanja { get; set; } = string.Empty;
        public string IzvozPutanja { get; set; } = string.Empty;
    }
}
