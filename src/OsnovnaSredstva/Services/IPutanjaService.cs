namespace OsnovnaSredstva.Services;

public interface IPutanjaService
{
    string? DajFinPutanju();
    bool SnimiFinPutanju(string putanja);
    bool JeValidanFinFolder(string putanja);

    string? DajArhivaPutanju();
    bool SnimiArhivaPutanju(string putanja);

    string? DajIzvozPutanju();
    bool SnimiIzvozPutanju(string putanja);
}
