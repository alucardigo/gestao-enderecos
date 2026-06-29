using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
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

    private async Task<HttpClient> ClientAutenticadoAsync(string usuario = "ana")
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var pagina = await client.GetAsync("/Account/Login");
        var token = HtmlHelpers.ExtractAntiForgeryToken(await pagina.Content.ReadAsStringAsync());
        var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Login"] = usuario,
                ["Senha"] = Senha,
                ["__RequestVerificationToken"] = token,
            }));

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    private static async Task<string> CriarEnderecoAsync(HttpClient client, string logradouro)
    {
        var form = await client.GetAsync("/Enderecos/Create");
        var token = HtmlHelpers.ExtractAntiForgeryToken(await form.Content.ReadAsStringAsync());
        await client.PostAsync("/Enderecos/Create", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Cep"] = "40010000",
                ["Logradouro"] = logradouro,
                ["Numero"] = "200",
                ["Bairro"] = "Centro",
                ["Cidade"] = "Salvador",
                ["Uf"] = "BA",
                ["__RequestVerificationToken"] = token,
            }));

        // Usa a busca para localizar o endereço recém-criado (a lista é paginada).
        var html = await (await client.GetAsync($"/Enderecos?q={Uri.EscapeDataString(logradouro)}"))
            .Content.ReadAsStringAsync();
        return Regex.Match(html, $@"data-id=""(\d+)""\s+data-label=""{Regex.Escape(logradouro)}").Groups[1].Value;
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

    [Fact]
    public async Task Modelo_de_importacao_baixa_csv_com_cabecalho()
    {
        var client = await ClientAutenticadoAsync();

        var resp = await client.GetAsync("/Enderecos/ModeloImportacao");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("text/csv", resp.Content.Headers.ContentType?.MediaType);
        Assert.Contains("CEP,Logradouro,Número,Complemento,Bairro,Cidade,UF",
            await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Exportacao_e_reimportavel_sem_rejeicoes_round_trip()
    {
        var client = await ClientAutenticadoAsync("bruno"); // bruno tem 1 endereço (seed)

        var export = await client.GetAsync("/Enderecos/Exportar");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal("text/csv", export.Content.Headers.ContentType?.MediaType);
        var csv = await export.Content.ReadAsStringAsync();

        // Reimporta o CSV exportado: deve entrar 100%, sem rejeições (formato compatível).
        var form = await client.GetAsync("/Enderecos/Importar");
        var token = HtmlHelpers.ExtractAntiForgeryToken(await form.Content.ReadAsStringAsync());
        using var content = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(arquivo, "arquivo", "export.csv");
        content.Add(new StringContent(token), "__RequestVerificationToken");

        var resp = await client.PostAsync("/Enderecos/Importar", content);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("importado", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rejeitada", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Importa_csv_via_http_com_validos_e_invalidos()
    {
        var client = await ClientAutenticadoAsync("bruno");

        var form = await client.GetAsync("/Enderecos/Importar");
        form.EnsureSuccessStatusCode();
        var token = HtmlHelpers.ExtractAntiForgeryToken(await form.Content.ReadAsStringAsync());

        var csv = "CEP,Logradouro,Número,Complemento,Bairro,Cidade,UF\n" +
                  "80010000,Rua Importada,99,,Centro,Curitiba,PR\n" + // válida
                  "abc,Rua Ruim,1,,Centro,Cidade,SP";                 // inválida (CEP)

        using var content = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(arquivo, "arquivo", "enderecos.csv");
        content.Add(new StringContent(token), "__RequestVerificationToken");

        var resp = await client.PostAsync("/Enderecos/Importar", content);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("importado", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rejeitada", html, StringComparison.OrdinalIgnoreCase);

        var lista = await (await client.GetAsync("/Enderecos")).Content.ReadAsStringAsync();
        Assert.Contains("Rua Importada", lista);
        Assert.Contains("Curitiba", lista);
    }

    [Fact]
    public async Task Usuario_nao_acessa_endereco_de_outro_via_http()
    {
        // Ana cria um endereço e capturamos o id pela listagem.
        var ana = await ClientAutenticadoAsync("ana");
        var id = await CriarEnderecoAsync(ana, "Avenida Sete de Ana");
        Assert.NotEqual(string.Empty, id);

        // Bruno (autenticado) tenta ler e excluir o endereço da Ana via id na URL → 404.
        var bruno = await ClientAutenticadoAsync("bruno");

        var leitura = await bruno.GetAsync($"/Enderecos/Edit/{id}");
        Assert.Equal(HttpStatusCode.NotFound, leitura.StatusCode);

        var token = HtmlHelpers.ExtractAntiForgeryToken(
            await (await bruno.GetAsync("/Enderecos")).Content.ReadAsStringAsync());
        var exclusao = await bruno.PostAsync($"/Enderecos/Delete/{id}", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.NotFound, exclusao.StatusCode);
    }
}
