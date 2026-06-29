using System.Text.RegularExpressions;

namespace GestaoEnderecos.Tests.TestSupport;

public static partial class HtmlHelpers
{
    /// <summary>Extrai o token antiforgery (campo hidden) de uma página HTML.</summary>
    public static string ExtractAntiForgeryToken(string html)
    {
        var match = TokenRegex().Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException("Token antiforgery não encontrado no HTML.");
        }

        return match.Groups[1].Value;
    }

    [GeneratedRegex(@"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""")]
    private static partial Regex TokenRegex();
}
