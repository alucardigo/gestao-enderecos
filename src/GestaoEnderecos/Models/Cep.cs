namespace GestaoEnderecos.Models;

/// <summary>Normalização e formatação de CEP — ponto único de verdade para os dois sentidos.</summary>
public static class Cep
{
    /// <summary>Remove a máscara, deixando apenas os dígitos (ex.: "01001-000" → "01001000").</summary>
    public static string Normalizar(string? cep) =>
        new([.. (cep ?? string.Empty).Where(char.IsDigit)]);

    /// <summary>Formata 8 dígitos como "NNNNN-NNN" para exibição (ex.: "01001000" → "01001-000").</summary>
    public static string Formatar(string? cep) =>
        cep is { Length: 8 } ? $"{cep[..5]}-{cep[5..]}" : cep ?? string.Empty;
}
