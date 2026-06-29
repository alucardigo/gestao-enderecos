using System.Text.Json.Serialization;

namespace GestaoEnderecos.Services;

/// <summary>
/// DTO do JSON do ViaCEP. Modela apenas os campos usados; os extras (ddd, ibge, gia, siafi,
/// unidade, regiao) são ignorados. Atenção: a "cidade" vem no campo <c>localidade</c>.
/// </summary>
public sealed class ViaCepResponse
{
    [JsonPropertyName("logradouro")]
    public string? Logradouro { get; set; }

    [JsonPropertyName("complemento")]
    public string? Complemento { get; set; }

    [JsonPropertyName("bairro")]
    public string? Bairro { get; set; }

    [JsonPropertyName("localidade")]
    public string? Localidade { get; set; }

    [JsonPropertyName("uf")]
    public string? Uf { get; set; }

    /// <summary>Verdadeiro quando o CEP não existe. Pode chegar como bool ou string "true".</summary>
    [JsonPropertyName("erro")]
    [JsonConverter(typeof(TolerantBooleanConverter))]
    public bool Erro { get; set; }
}
