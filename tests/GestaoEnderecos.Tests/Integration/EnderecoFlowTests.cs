using System.Net;
using GestaoEnderecos.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GestaoEnderecos.Tests.Integration;

/// <summary>
/// Fluxo autenticado ponta a ponta sobre a aplicação real: login → criar → listar → exportar,
/// e o endpoint de busca de CEP. Valida o caminho crítico do produto, não só unidades.
/// </summary>
public class EnderecoFlowTests : IClassFixture<GestaoWebAppFactory>
{
    private const string Senha = "Senha@123";
    private readonly GestaoWebAppFactory _factory;

    public EnderecoFlowTests(GestaoWebAppFactory factory)
    {
        _factory = factory;
        _factory.Seed();
    }

    private async Task<HttpClient> ClientAutenticadoAsync()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var pagina = await client.GetAsync("/Account/Login");
        var token = HtmlHelpers.ExtractAntiForgeryToken(await pagina.Content.ReadAsStringAsync());
        var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Login"] = "ana",
                ["Senha"] = Senha,
                ["__RequestVerificationToken"] = token,
            }));

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    [Fact]
    public async Task Criar_listar_e_exportar_enderecos()
    {
        var client = await ClientAutenticadoAsync();

        var formCreate = await client.GetAsync("/Enderecos/Create");
        formCreate.EnsureSuccessStatusCode();
        var token = HtmlHelpers.ExtractAntiForgeryToken(await formCreate.Content.ReadAsStringAsync());

        var criar = await client.PostAsync("/Enderecos/Create", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Cep"] = "30140-071",
                ["Logradouro"] = "Rua da Bahia",
                ["Numero"] = "1000",
                ["Bairro"] = "Centro",
                ["Cidade"] = "Belo Horizonte",
                ["Uf"] = "MG",
                ["__RequestVerificationToken"] = token,
            }));
        Assert.Equal(HttpStatusCode.Redirect, criar.StatusCode);

        var lista = await client.GetAsync("/Enderecos");
        lista.EnsureSuccessStatusCode();
        var html = await lista.Content.ReadAsStringAsync();
        Assert.Contains("Rua da Bahia", html);
        Assert.Contains("Belo Horizonte", html);

        var export = await client.GetAsync("/Enderecos/Exportar");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal("text/csv", export.Content.Headers.ContentType?.MediaType);
        var csv = await export.Content.ReadAsStringAsync();
        Assert.Contains("Rua da Bahia", csv);
        Assert.Contains("30140-071", csv); // CEP formatado
    }

    [Fact]
    public async Task BuscarCep_existente_retorna_json_com_o_endereco()
    {
        var client = await ClientAutenticadoAsync();

        var resposta = await client.GetAsync("/Enderecos/BuscarCep?cep=01001000");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var json = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("Praça da Sé", json);
        Assert.Contains("São Paulo", json);
    }

    [Fact]
    public async Task BuscarCep_inexistente_retorna_404()
    {
        var client = await ClientAutenticadoAsync();

        var resposta = await client.GetAsync("/Enderecos/BuscarCep?cep=99999999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
