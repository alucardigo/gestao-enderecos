using System.Net;
using GestaoEnderecos.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GestaoEnderecos.Tests.Integration;

/// <summary>
/// Autorização da área administrativa e criação de usuários por administrador (não há autocadastro:
/// novas contas são criadas apenas por um administrador).
/// </summary>
public class UsuarioFlowTests : IClassFixture<GestaoWebAppFactory>
{
    private const string Senha = "Senha@123";
    private readonly GestaoWebAppFactory _factory;

    public UsuarioFlowTests(GestaoWebAppFactory factory)
    {
        _factory = factory;
        _factory.Seed();
    }

    private HttpClient CriarClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<HttpClient> LogarAsync(string usuario)
    {
        var client = CriarClient();
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

    private static async Task<HttpResponseMessage> CriarUsuarioAsync(
        HttpClient adminClient, string nome, string login, string senha)
    {
        var form = await adminClient.GetAsync("/Usuarios/Create");
        form.EnsureSuccessStatusCode();
        var token = HtmlHelpers.ExtractAntiForgeryToken(await form.Content.ReadAsStringAsync());
        return await adminClient.PostAsync("/Usuarios/Create", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Nome"] = nome,
                ["Login"] = login,
                ["Senha"] = senha,
                ["__RequestVerificationToken"] = token,
            }));
    }

    [Fact]
    public async Task Nao_existe_rota_publica_de_autocadastro()
    {
        var client = CriarClient();

        var resposta = await client.GetAsync("/Account/Register");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Admin_cria_novo_usuario()
    {
        var ana = await LogarAsync("ana");

        var resposta = await CriarUsuarioAsync(ana, "Carlos Teste", "carlos", "Senha@123");

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        var lista = await (await ana.GetAsync("/Usuarios")).Content.ReadAsStringAsync();
        Assert.Contains("Carlos Teste", lista);
    }

    [Fact]
    public async Task Admin_create_rejeita_senha_fraca()
    {
        var ana = await LogarAsync("ana");

        var resposta = await CriarUsuarioAsync(ana, "Fraco", "fraco", "123456");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode); // volta com erro de validação
        Assert.Contains("8 caracteres", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_create_rejeita_login_duplicado()
    {
        var ana = await LogarAsync("ana");

        var resposta = await CriarUsuarioAsync(ana, "Outro Bruno", "bruno", "Senha@123");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Já existe um usuário", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Usuario_comum_nao_acessa_area_administrativa()
    {
        var bruno = await LogarAsync("bruno"); // não-admin

        var resposta = await bruno.GetAsync("/Usuarios");

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Account/AccessDenied", resposta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Administrador_acessa_a_lista_de_usuarios()
    {
        var ana = await LogarAsync("ana"); // admin (seed)

        var resposta = await ana.GetAsync("/Usuarios");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var html = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("Bruno Lima", html);
    }
}
