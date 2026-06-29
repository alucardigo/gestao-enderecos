using GestaoEnderecos.Services;

namespace GestaoEnderecos.Tests.Unit;

/// <summary>T6 — normalização do CEP (dado consistente, RN-03).</summary>
public class EnderecoServiceTests
{
    [Theory]
    [InlineData("01001-000", "01001000")]
    [InlineData("01001000", "01001000")]
    [InlineData("  01001-000 ", "01001000")]
    [InlineData("01.001-000", "01001000")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void NormalizarCep_remove_mascara_e_deixa_apenas_digitos(string? entrada, string esperado)
    {
        Assert.Equal(esperado, EnderecoService.NormalizarCep(entrada));
    }
}
