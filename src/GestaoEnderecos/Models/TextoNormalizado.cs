using System.Globalization;
using System.Text;

namespace GestaoEnderecos.Models;

/// <summary>
/// Normalização de texto para busca tolerante: minúsculas + remoção de acentos. Assim "São",
/// "sao" e "SAO" passam a ser equivalentes (importante porque o LIKE do SQLite é sensível a
/// maiúsculas/acentos).
/// </summary>
public static class TextoNormalizado
{
    /// <summary>Texto de busca consolidado de um endereço (todos os campos relevantes).</summary>
    public static string Para(Endereco e) =>
        Normalizar($"{e.Logradouro} {e.Numero} {e.Complemento} {e.Bairro} {e.Cidade} {e.Uf} {e.Cep}");

    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
    }
}
