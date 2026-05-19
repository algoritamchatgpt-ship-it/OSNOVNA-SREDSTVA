using System.Globalization;
using System.IO;
using System.Text;

namespace OsnovnaSredstva.Services.Dbf;

public static class DbfTableWriter
{
    public sealed record DbfField(string Name, char Type, int Length, int Decimals, int Offset);

    public sealed class DbfSchema
    {
        public required string TemplatePath { get; init; }
        public required byte Version { get; init; }
        public required byte CodePageMark { get; init; }
        public required ushort HeaderLength { get; init; }
        public required ushort RecordLength { get; init; }
        public required byte[] HeaderBytes { get; init; }
        public required IReadOnlyList<DbfField> Fields { get; init; }
        public required Encoding Encoding { get; init; }
    }

    public static DbfSchema LoadSchema(string templatePath)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("DBF template nije pronadjen.", templatePath);

        using var fs = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var br = new BinaryReader(fs, Encoding.ASCII);

        var version = br.ReadByte();
        br.ReadBytes(3);
        br.ReadUInt32();
        var headerLen = br.ReadUInt16();
        var recordLen = br.ReadUInt16();
        br.ReadBytes(16);
        br.ReadByte();
        var codePage = br.ReadByte();
        br.ReadBytes(2);

        fs.Seek(0, SeekOrigin.Begin);
        var headerBytes = br.ReadBytes(headerLen);
        if (headerBytes.Length != headerLen)
            throw new InvalidDataException("Neispravan DBF header.");

        fs.Seek(32, SeekOrigin.Begin);
        var fields = new List<DbfField>();
        var offset = 1;
        while (true)
        {
            var first = br.ReadByte();
            if (first == 0x0D)
                break;

            var rest = br.ReadBytes(31);
            if (rest.Length < 31)
                throw new InvalidDataException("Neispravan opis DBF polja.");

            var nameBytes = new byte[11];
            nameBytes[0] = first;
            Array.Copy(rest, 0, nameBytes, 1, 10);
            var name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0').Trim();
            var type = (char)rest[10];
            var length = rest[15];
            var decimals = rest[16];

            fields.Add(new DbfField(name, type, length, decimals, offset));
            offset += length;
        }

        return new DbfSchema
        {
            TemplatePath = templatePath,
            Version = version,
            CodePageMark = codePage,
            HeaderLength = headerLen,
            RecordLength = recordLen,
            HeaderBytes = headerBytes,
            Fields = fields,
            Encoding = MapEncoding(codePage)
        };
    }

    public static void WriteTable<T>(
        string targetPath,
        DbfSchema schema,
        IReadOnlyList<T> rows,
        Func<T, string, object?> valueResolver)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        var header = (byte[])schema.HeaderBytes.Clone();
        var now = DateTime.Now;
        header[1] = (byte)(now.Year - 1900);
        header[2] = (byte)now.Month;
        header[3] = (byte)now.Day;

        var countBytes = BitConverter.GetBytes((uint)rows.Count);
        Array.Copy(countBytes, 0, header, 4, 4);

        using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        fs.Write(header, 0, header.Length);

        foreach (var row in rows)
        {
            var record = Enumerable.Repeat((byte)0x20, schema.RecordLength).ToArray();
            record[0] = 0x20;

            foreach (var field in schema.Fields)
            {
                var value = valueResolver(row, field.Name);
                var bytes = FormatField(field, value, schema.Encoding);
                Array.Copy(bytes, 0, record, field.Offset, Math.Min(bytes.Length, field.Length));
            }

            fs.Write(record, 0, record.Length);
        }

        fs.WriteByte(0x1A);
    }

    private static byte[] FormatField(DbfField field, object? value, Encoding enc)
    {
        var text = field.Type switch
        {
            'C' => FormatCharacter(value, field.Length),
            'N' => FormatNumeric(value, field.Length, field.Decimals),
            'F' => FormatNumeric(value, field.Length, field.Decimals),
            'D' => FormatDate(value),
            'L' => FormatLogical(value),
            _ => new string(' ', field.Length)
        };

        var bytes = enc.GetBytes(text);
        if (bytes.Length == field.Length)
            return bytes;

        if (bytes.Length < field.Length)
        {
            var padded = new byte[field.Length];
            Array.Fill(padded, (byte)0x20);
            Array.Copy(bytes, 0, padded, 0, bytes.Length);
            return padded;
        }

        var truncated = new byte[field.Length];
        Array.Copy(bytes, 0, truncated, 0, field.Length);
        return truncated;
    }

    private static string FormatCharacter(object? value, int len)
    {
        var text = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
        if (text.Length > len)
            return text[..len];
        return text.PadRight(len, ' ');
    }

    private static string FormatNumeric(object? value, int len, int decimals)
    {
        if (value is null)
            return new string(' ', len);

        decimal d;
        try { d = Convert.ToDecimal(value, CultureInfo.InvariantCulture); }
        catch { return new string(' ', len); }

        d = Math.Round(d, decimals, MidpointRounding.AwayFromZero);
        var format = decimals > 0 ? $"F{decimals}" : "F0";
        var text = d.ToString(format, CultureInfo.InvariantCulture);

        if (text.Length > len) return new string('*', len);
        return text.PadLeft(len, ' ');
    }

    private static string FormatDate(object? value)
    {
        if (value is DateTime dt)
            return dt.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        if (value is DateTimeOffset dto)
            return dto.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        if (value is string s && DateTime.TryParse(s, out var parsed))
            return parsed.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return "        ";
    }

    private static string FormatLogical(object? value)
    {
        if (value is bool b) return b ? "T" : "F";
        if (value is string s)
        {
            var upper = s.Trim().ToUpperInvariant();
            if (upper is "T" or "Y" or ".T." or "1" or "D") return "T";
            if (upper is "F" or "N" or ".F." or "0") return "F";
        }
        return "?";
    }

    private static Encoding MapEncoding(byte codePage) => codePage switch
    {
        0x01 => Encoding.GetEncoding(437),
        0x02 => Encoding.GetEncoding(850),
        0x03 => Encoding.GetEncoding(1252),
        0x64 => Encoding.GetEncoding(852),
        0x65 => Encoding.GetEncoding(866),
        0xC8 => Encoding.GetEncoding(1250),
        0xC9 => Encoding.GetEncoding(1251),
        0xCB => Encoding.GetEncoding(1253),
        _ => Encoding.GetEncoding(1250),
    };
}
