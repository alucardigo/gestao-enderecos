namespace GestaoEnderecos.Models;

/// <summary>Lista canônica das 27 unidades federativas do Brasil (validação de UF).</summary>
public static class UnidadesFederativas
{
    public static readonly IReadOnlySet<string> Todas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG",
        "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    };

    public static bool EhValida(string? uf) =>
        !string.IsNullOrWhiteSpace(uf) && Todas.Contains(uf.Trim());
}
