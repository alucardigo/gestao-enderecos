namespace GestaoEnderecos.Models;

/// <summary>
/// Similaridade textual para busca tolerante a erros de digitação (Damerau-Levenshtein, que conta
/// troca de letras adjacentes — ex.: "rau" ↔ "rua" — como distância 1).
/// </summary>
public static class Similaridade
{
    /// <summary>Menor distância entre o termo e qualquer palavra (token) do texto.</summary>
    public static int MenorDistanciaPorToken(string textoNormalizado, string termoNormalizado)
    {
        var melhor = int.MaxValue;
        foreach (var token in textoNormalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var d = DamerauLevenshtein(token, termoNormalizado);
            if (d < melhor)
            {
                melhor = d;
            }
        }

        return melhor;
    }

    public static int DamerauLevenshtein(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var custo = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + custo);

                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                {
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1); // transposição
                }
            }
        }

        return d[a.Length, b.Length];
    }
}
