using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Musify.Application.Serialization;

// Zitadel-issued user ids routinely exceed Number.MAX_SAFE_INTEGER (2^53-1).
// Serialized as a raw JSON number, the frontend's JSON.parse silently rounds
// them to the nearest representable double, corrupting the id (it becomes a
// different, sometimes valid, user id). Applied via [property: JsonConverter(...)]
// on the specific id-shaped `long`/`long?` response properties (not globally —
// unrelated `long` fields like byte sizes must stay numbers), this serializes
// them as a JSON string instead, which round-trips exactly.
public sealed class LongAsStringConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? long.Parse(reader.GetString()!, CultureInfo.InvariantCulture)
            : reader.GetInt64();

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}

public sealed class NullableLongAsStringConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        return reader.TokenType == JsonTokenType.String
            ? string.IsNullOrEmpty(reader.GetString()) ? null : long.Parse(reader.GetString()!, CultureInfo.InvariantCulture)
            : reader.GetInt64();
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
        else
            writer.WriteNullValue();
    }
}
