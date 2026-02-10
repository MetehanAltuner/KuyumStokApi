using System.Text.Json;
using System.Text.Json.Serialization;

namespace KuyumStokApi.API.Converters;

/// <summary>
/// DateTimeOffset ve DateTimeOffset? değerlerini UTC formatında "yyyy-MM-dd HH:mm:ss" formatına dönüştürür.
/// </summary>
public class UtcDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue))
                return default;

            // Parse ederken UTC olarak kabul et
            if (DateTimeOffset.TryParse(stringValue, out var dateTimeOffset))
            {
                return dateTimeOffset.ToUniversalTime();
            }
        }

        // Fallback: ISO formatını da destekle
        return reader.GetDateTimeOffset().ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // UTC'ye çevir ve formatla
        var utcValue = value.ToUniversalTime();
        writer.WriteStringValue(utcValue.ToString(Format));
    }
}

/// <summary>
/// Nullable DateTimeOffset değerlerini UTC formatında "yyyy-MM-dd HH:mm:ss" formatına dönüştürür.
/// </summary>
public class NullableUtcDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset?>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            // Parse ederken UTC olarak kabul et
            if (DateTimeOffset.TryParse(stringValue, out var dateTimeOffset))
            {
                return dateTimeOffset.ToUniversalTime();
            }
        }

        // Fallback: ISO formatını da destekle
        return reader.GetDateTimeOffset().ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // UTC'ye çevir ve formatla
        var utcValue = value.Value.ToUniversalTime();
        writer.WriteStringValue(utcValue.ToString(Format));
    }
}
