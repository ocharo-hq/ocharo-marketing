using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace OcharoBlog.Persistence.Import;

/// <summary>A table's row count and the SHA-256 over its rows in canonical form.</summary>
/// <param name="Rows">The number of rows.</param>
/// <param name="Sha256">The lowercase hex SHA-256 of the canonical rows, sorted by key, one per line.</param>
public sealed record ImportDigest(int Rows, string Sha256)
{
    private static readonly JsonWriterOptions Canonical = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
    };

    /// <summary>Hashes rows read from either side, each value already in its canonical .NET form: a string for text and
    /// documents, a <see cref="long"/> for integers and for instants (microseconds since the Unix epoch), or
    /// null.</summary> <remarks>
    ///   - each row is a JSON array of its values; a document is embedded with its object keys sorted (the last of a
    ///     repeated key wins, as in <c>jsonb</c>) and its numbers in PostgreSQL <c>numeric</c>'s plain form, so a
    ///     document reads alike before and after <c>jsonb</c> normalises it
    ///   - rows sort by their key values' canonical text, ordinally, so neither side's collation decides the order
    /// </remarks>
    /// <param name="table">The table the rows belong to.</param>
    /// <param name="rows">The rows, one value per column of <paramref name="table"/>.</param>
    /// <exception cref="JsonException">A document is not JSON.</exception>
    public static ImportDigest Of(ImportTable table, IReadOnlyList<object?[]> rows)
    {
        var lines = new List<(string Key, byte[] Row)>(rows.Count);
        foreach (var row in rows)
            lines.Add((Encoding.UTF8.GetString(Write(table, row, table.KeyCount)), Write(table, row, row.Length)));
        lines.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var (_, row) in lines)
        {
            hash.AppendData(row);
            hash.AppendData("\n"u8);
        }
        return new ImportDigest(rows.Count, Convert.ToHexStringLower(hash.GetHashAndReset()));
    }

    /// <summary>Writes the first <paramref name="count"/> values of a row as a canonical JSON array.</summary>
    private static byte[] Write(ImportTable table, object?[] row, int count)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, Canonical))
        {
            writer.WriteStartArray();
            for (var index = 0; index < count; index++)
            {
                switch (row[index])
                {
                    case null:
                        writer.WriteNullValue();
                        break;
                    case long number:
                        writer.WriteNumberValue(number);
                        break;
                    case string text when table.Columns[index].Kind == ImportColumnKind.Document:
                        using (var document = JsonDocument.Parse(text))
                            WriteDocument(writer, document.RootElement);
                        break;
                    case string text:
                        writer.WriteStringValue(text);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Column {table.Columns[index].Target} holds no canonical value.");
                }
            }
            writer.WriteEndArray();
        }
        return buffer.ToArray();
    }

    /// <summary>Writes a JSON value with sorted object keys and plain numbers.</summary>
    private static void WriteDocument(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var properties = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                    properties[property.Name] = property.Value;
                writer.WriteStartObject();
                foreach (var (name, value) in properties)
                {
                    writer.WritePropertyName(name);
                    WriteDocument(writer, value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteDocument(writer, item);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(Number(element.GetRawText()), skipInputValidation: true);
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                writer.WriteBooleanValue(element.GetBoolean());
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }

    /// <summary>Spells a JSON number as PostgreSQL <c>numeric</c> prints it: plain digits, no exponent, the input's
    /// fractional digits less its exponent as the scale, and no negative zero.</summary>
    internal static string Number(string raw)
    {
        var negative = raw.StartsWith('-');
        var body = negative ? raw[1..] : raw;
        var exponentAt = body.IndexOfAny(['e', 'E']);
        var exponent = exponentAt < 0 ? 0 : int.Parse(body[(exponentAt + 1)..], NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture);
        var mantissa = exponentAt < 0 ? body : body[..exponentAt];
        var point = mantissa.IndexOf('.');
        var fraction = point < 0 ? string.Empty : mantissa[(point + 1)..];
        var digits = (point < 0 ? mantissa : mantissa[..point]) + fraction;

        // The value is digits × 10^-scale; a negative scale becomes trailing zeros.
        var scale = fraction.Length - exponent;
        if (scale < 0)
        {
            digits += new string('0', -scale);
            scale = 0;
        }
        if (digits.Length <= scale)
            digits = new string('0', scale - digits.Length + 1) + digits;

        var whole = digits[..^scale].TrimStart('0');
        var text = (whole.Length == 0 ? "0" : whole) + (scale > 0 ? "." + digits[^scale..] : string.Empty);
        return negative && digits.Any(digit => digit != '0') ? "-" + text : text;
    }
}
