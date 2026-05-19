using OsnovnaSredstva.Models;

namespace OsnovnaSredstva.Services;

public interface IFirmaService
{
    Task<List<Firma>> DajSveFirmeAsync();
    Task<Firma?> DodajFirmuAsync();
    Task<bool> ObrisiFirmuAsync(string folderPath);
}
