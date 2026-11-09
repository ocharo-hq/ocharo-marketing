using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Npgsql;
using NpgsqlTypes;

namespace OcharoBlog.Persistence.Access;

/// <summary>Keeps the Data Protection key ring in <c>blog.data_protection_keys</c>, so every replica and restart opens
/// the session cookies the last one sealed.</summary>
/// <remarks>The key manager only appends key and revocation elements and reads them all back; the table is append-only.
/// The interface is synchronous, so the calls block on the pool.</remarks>
public sealed class PostgresXmlRepository(NpgsqlDataSource dataSource) : IXmlRepository
{
    /// <inheritdoc />
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var command = dataSource.CreateCommand(
            $"SELECT xml FROM {BlogSchema.Name}.data_protection_keys ORDER BY id");
        using var reader = command.ExecuteReader();
        var elements = new List<XElement>();
        while (reader.Read())
            elements.Add(XElement.Parse(reader.GetString(0)));
        return elements;
    }

    /// <inheritdoc />
    public void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);
        using var command = dataSource.CreateCommand(
            $"INSERT INTO {BlogSchema.Name}.data_protection_keys (friendly_name, xml) VALUES ($1, $2)");
        command.Parameters.Add(new NpgsqlParameter { Value = friendlyName, NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter
        {
            Value = element.ToString(SaveOptions.DisableFormatting),
            NpgsqlDbType = NpgsqlDbType.Text,
        });
        command.ExecuteNonQuery();
    }
}
