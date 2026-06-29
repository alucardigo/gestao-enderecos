using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestaoEnderecos.Services;

/// <summary>
/// Lê um booleano que pode chegar como bool (<c>true</c>) ou string (<c>"true"</c>). O ViaCEP,
/// na versão atual, devolve o campo <c>erro</c> como a STRING "true" — daí a tolerância.
/// </summary>
public sealed class TolerantBooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => bool.TryParse(reader.GetString(), out var valor) && valor,
            JsonTokenType.Number => reader.GetInt32() != 0,
            _ => false,
        };

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
        writer.WriteBooleanValue(value);
}
