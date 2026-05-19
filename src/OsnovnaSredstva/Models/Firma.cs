namespace OsnovnaSredstva.Models;

public class Firma
{
    public string FolderPath { get; set; } = string.Empty;
    public string FolderIme { get; set; } = string.Empty;
    public bool Aktivna { get; set; }

    // Osnovni podaci
    public string Naziv { get; set; } = string.Empty;
    public string Naziv2 { get; set; } = string.Empty;
    public string NazivLatinican { get; set; } = string.Empty;
    public string Baza { get; set; } = string.Empty;
    public string Vlasnik { get; set; } = string.Empty;
    public string OdgovornoLice { get; set; } = string.Empty;
    public string OrganizacioniOblik { get; set; } = string.Empty;
    public string Maticni { get; set; } = string.Empty;
    public string MatBr { get; set; } = string.Empty;
    public string Pib { get; set; } = string.Empty;
    public string PdvObveznik { get; set; } = string.Empty;
    public string SifraDelatnosti { get; set; } = string.Empty;
    public string NazivDelatnosti { get; set; } = string.Empty;

    // Adresa
    public string PostanskiBroj { get; set; } = string.Empty;
    public string Mesto { get; set; } = string.Empty;
    public string Ulica { get; set; } = string.Empty;
    public string BrojUlice { get; set; } = string.Empty;
    public string Opstina { get; set; } = string.Empty;
    public string Republika { get; set; } = string.Empty;
    public string Drzava { get; set; } = string.Empty;

    // Kontakt
    public string Telefon1 { get; set; } = string.Empty;
    public string Telefon2 { get; set; } = string.Empty;
    public string Fax1 { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Web { get; set; } = string.Empty;
    public string Agencija { get; set; } = string.Empty;

    // Žiro računi
    public string ZiroRacun { get; set; } = string.Empty;
    public string ZiroRacun2 { get; set; } = string.Empty;
    public string ZiroRacunDevizni { get; set; } = string.Empty;
    public string ZiroRacunBolovanje { get; set; } = string.Empty;

    // Banke
    public string Banka1 { get; set; } = string.Empty;
    public string Banka2 { get; set; } = string.Empty;
    public string BankaDevizna { get; set; } = string.Empty;
    public string BankaBolovanje { get; set; } = string.Empty;
    public string SwiftKod { get; set; } = string.Empty;

    // Datumi registracije
    public DateTime? DatumOsnivanja { get; set; }
    public DateTime? DatumRegistracije { get; set; }
    public DateTime? DatumUpisa { get; set; }
    public DateTime? DatumPdv { get; set; }

    // Registracioni brojevi
    public string RegBrojSocijalno { get; set; } = string.Empty;
    public string RegBrojZdravstveno { get; set; } = string.Empty;
    public string SudskiRegistar { get; set; } = string.Empty;

    public string PrikazniNaziv =>
        string.IsNullOrWhiteSpace(Naziv) ? FolderIme : $"{FolderIme} — {Naziv}";

    public string AdresaUlica =>
        string.Join(" ", new[] { Ulica.Trim(), BrojUlice.Trim() }.Where(s => !string.IsNullOrEmpty(s)));

    public string AdresaMesto =>
        string.Join(", ", new[] { PostanskiBroj.Trim(), Mesto.Trim() }.Where(s => !string.IsNullOrEmpty(s)));
}
