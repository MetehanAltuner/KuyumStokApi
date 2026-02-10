using System.Text.Json;
using System.Text.Json.Serialization;

namespace KuyumStokApi.API.Converters;

/// <summary>
/// DateTime ve DateTime? değerlerini UTC formatında "yyyy-MM-dd HH:mm:ss" formatına dönüştürür.
/// </summary>
public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue))
                return default;

            // Parse ederken UTC olarak kabul et
            if (DateTime.TryParse(stringValue, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dateTime))
            {
                return dateTime.ToUniversalTime();
            }
        }

        // Fallback: ISO formatını da destekle
        return reader.GetDateTime().ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // UTC'ye çevir ve formatla
        var utcValue = value.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc) 
            : value.ToUniversalTime();
        
        writer.WriteStringValue(utcValue.ToString(Format));
    }
}

/// <summary>
/// Nullable DateTime değerlerini UTC formatında "yyyy-MM-dd HH:mm:ss" formatına dönüştürür.
/// </summary>
public class NullableUtcDateTimeJsonConverter : JsonConverter<DateTime?>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            // Parse ederken UTC olarak kabul et
            if (DateTime.TryParse(stringValue, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dateTime))
            {
                return dateTime.ToUniversalTime();
            }
        }

        // Fallback: ISO formatını da destekle
        return reader.GetDateTime().ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // UTC'ye çevir ve formatla
        var utcValue = value.Value.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) 
            : value.Value.ToUniversalTime();
        
        writer.WriteStringValue(utcValue.ToString(Format));
    }
}
