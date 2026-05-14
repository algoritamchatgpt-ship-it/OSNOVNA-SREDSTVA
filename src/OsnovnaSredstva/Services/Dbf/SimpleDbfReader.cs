using System.IO;
using System.Text;

namespace OsnovnaSredstva.Services.Dbf;

/// <summary>
/// Čita FoxPro/dBASE DBF fajlove. Podržava C, N, L, D tipove polja.
/// </summary>
public class SimpleDbfReader
{
    private readonly List<DbfField> _fields;
    private readonly int _recordSize;
    private readonly int _headerSize;
    private readonly int _recordCount;
    private readonly byte[] _data;
    private readonly Encoding _encoding;

    public IReadOnlyList<DbfField> Fields => _fields;
    public int RecordCount => _recordCount;

    public SimpleDbfReader(string filePath, Encoding? encoding = null)
    {
        _encoding = encoding ?? Encoding.GetEncoding(1250);
        _data = File.ReadAllBytes(filePath);

        if (_data.Length < 32)
            throw new InvalidDataException("DBF fajl je prekratak.");

        _recordCount = BitConverter.ToInt32(_data, 4);
        _headerSize = BitConverter.ToInt16(_data, 8);
        _recordSize = BitConverter.ToInt16(_data, 10);

        _fields = [];
        int offset = 32;
        int fieldOffset = 1; // bajt 0 = deletion flag

        while (offset < _headerSize - 1 && _data[offset] != 0x0D)
        {
            if (offset + 32 > _data.Length) break;

            var nameBytes = _data[offset..(offset + 11)];
            var nameEnd = Array.IndexOf(nameBytes, (byte)0);
            var name = _encoding.GetString(nameBytes, 0, nameEnd < 0 ? 11 : nameEnd).TrimEnd('\0');
            var type = (char)_data[offset + 11];
            var length = _data[offset + 16];
            var decimals = _data[offset + 17];

            _fields.Add(new DbfField(name, type, fieldOffset, length, decimals));
            fieldOffset += length;
            offset += 32;
        }
    }

    public IEnumerable<DbfRecord> Zapisi()
    {
        for (int i = 0; i < _recordCount; i++)
        {
            int pos = _headerSize + i * _recordSize;
            if (pos >= _data.Length) break;

            bool obrisan = _data[pos] == 0x2A;
            if (obrisan) continue;

            var record = new DbfRecord(_fields, _data, pos, _encoding);
            yield return record;
        }
    }
}

public class DbfField
{
    public string Name { get; }
    public char Type { get; }
    public int Offset { get; }
    public int Length { get; }
    public int Decimals { get; }

    public DbfField(string name, char type, int offset, int length, int decimals)
    {
        Name = name.ToUpperInvariant();
        Type = char.ToUpperInvariant(type);
        Offset = offset;
        Length = length;
        Decimals = decimals;
    }
}

public class DbfRecord
{
    private readonly List<DbfField> _fields;
    private readonly byte[] _data;
    private readonly int _recordStart;
    private readonly Encoding _encoding;

    public DbfRecord(List<DbfField> fields, byte[] data, int recordStart, Encoding encoding)
    {
        _fields = fields;
        _data = data;
        _recordStart = recordStart;
        _encoding = encoding;
    }

    public string DajString(string fieldName)
    {
        var field = _fields.FirstOrDefault(f =>
            string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        if (field is null) return string.Empty;

        int pos = _recordStart + field.Offset;
        if (pos + field.Length > _data.Length) return string.Empty;

        return _encoding.GetString(_data, pos, field.Length).Trim();
    }

    public bool DajBool(string fieldName)
    {
        var val = DajString(fieldName).ToUpperInvariant();
        return val is "T" or "Y" or "1";
    }

    public int DajInt(string fieldName)
    {
        var val = DajString(fieldName);
        return int.TryParse(val, out var n) ? n : 0;
    }

    public decimal DajDecimal(string fieldName)
    {
        var val = DajString(fieldName);
        return decimal.TryParse(val, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    public DateTime? DajDate(string fieldName)
    {
        var s = DajString(fieldName).Trim();
        if (s.Length == 8 && DateTime.TryParseExact(s, "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt))
            return dt;
        return null;
    }
}
