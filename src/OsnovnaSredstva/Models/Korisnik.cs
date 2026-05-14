namespace OsnovnaSredstva.Models;

public class Korisnik
{
    public string Pas { get; set; } = string.Empty;
    public string KorisnikIme { get; set; } = string.Empty;
    public string KorisnikIme2 { get; set; } = string.Empty;
    public string Lozinka { get; set; } = string.Empty;
    public bool Aktivan { get; set; }
    public bool JeSupervizor { get; set; }
    public int PravaNivo { get; set; }

    // Prava pristupa po modulima
    public bool PassGk { get; set; }
    public bool PassAn { get; set; }
    public bool PassBl { get; set; }
    public bool PassTv { get; set; }
    public bool PassTm { get; set; }
    public bool PassUs { get; set; }
    public bool PassLd { get; set; }
    public bool PassOst { get; set; }
    public bool PassPrn { get; set; }
    public bool PassPro { get; set; }
    public bool PassOs { get; set; }
    public bool PassProf { get; set; }
    public bool PassDel { get; set; }
}
