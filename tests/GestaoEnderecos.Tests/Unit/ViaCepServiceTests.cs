using System.Net;
using GestaoEnderecos.Services;
using GestaoEnderecos.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace GestaoEnderecos.Tests.Unit;

/// <summary>T1/T2/T3 — integração com o ViaCEP isolada por um handler HTTP de teste.</summary>
public class ViaCepServiceTests
{
    private static ViaCepService Criar(StubHttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://viacep.com.br/") },
            NullLogger<ViaCepService>.Instance);

    [Fact] // T1
    public async Task Cep_valido_mapeia_localidade_para_cidade()
    {
        const string json = """
        {"cep":"01001-000","logradouro":"Praça da Sé","complemento":"lado ímpar",
         "bairro":"Sé","localidade":"São Paulo","uf":"SP"}
        """;
        var service = Criar(StubHttpMessageHandler.ComJson(json));

        var resultado = await service.BuscarAsync("01001-000");

        Assert.NotNull(resultado);
        Assert.Equal("01001000", resultado!.Cep);
        Assert.Equal("Praça da Sé", resultado.Logradouro);
        Assert.Equal("São Paulo", resultado.Cidade); // veio de "localidade"
        Assert.Equal("SP", resultado.Uf);
    }

    [Fact] // T2 — erro como STRING "true"
    public async Task Cep_inexistente_com_erro_string_retorna_null()
    {
        var service = Criar(StubHttpMessageHandler.ComJson("""{"erro":"true"}"""));

        Assert.Null(await service.BuscarAsync("99999999"));
    }

    [Fact] // T2b — erro como bool true (robustez)
    public async Task Cep_inexistente_com_erro_bool_retorna_null()
    {
        var service = Criar(StubHttpMessageHandler.ComJson("""{"erro":true}"""));

        Assert.Null(await service.BuscarAsync("99999999"));
    }

    [Fact] // T3 — status 500
    public async Task Status_500_degrada_para_null()
    {
        var service = Criar(StubHttpMessageHandler.ComStatus(HttpStatusCode.InternalServerError));

        Assert.Null(await service.BuscarAsync("01001000"));
    }

    [Fact] // T3 — timeout (TaskCanceledException)
    public async Task Timeout_degrada_para_null()
    {
        var service = Criar(StubHttpMessageHandler.Lancando(new TaskCanceledException()));

        Assert.Null(await service.BuscarAsync("01001000"));
    }

    [Fact] // T3 — indisponibilidade de rede
    public async Task Falha_de_rede_degrada_para_null()
    {
        var service = Criar(StubHttpMessageHandler.Lancando(new HttpRequestException("DNS")));

        Assert.Null(await service.BuscarAsync("01001000"));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    [InlineData("abcdefgh")]
    public async Task Cep_invalido_nem_chama_a_api(string cepInvalido)
    {
        // Handler que falharia se chamado — prova que CEP inválido é barrado antes da rede.
        var service = Criar(StubHttpMessageHandler.Lancando(new InvalidOperationException("não deveria chamar")));

        Assert.Null(await service.BuscarAsync(cepInvalido));
    }
}
