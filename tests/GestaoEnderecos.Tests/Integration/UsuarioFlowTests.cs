using System.Net;
using GestaoEnderecos.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GestaoEnderecos.Tests.Integration;

/// <summary>Cadastro de usuário e autorização da área administrativa (papel Admin).</summary>
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

    [Fact]
    public async Task Registro_cria_conta_e_ja_entra_autenticado()
    {
        var client = CriarClient();
        var pagina = await client.GetAsync("/Account/Register");
        pagina.EnsureSuccessStatusCode();
        var token = HtmlHelpers.ExtractAntiForgeryToken(await pagina.Content.ReadAsStringAsync());

        var registro = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Nome"] = "Carlos Teste",
                ["Login"] = "carlos",
                ["Senha"] = "Senha@123",
                ["ConfirmarSenha"] = "Senha@123",
                ["__RequestVerificationToken"] = token,
            }));
        Assert.Equal(HttpStatusCode.Redirect, registro.StatusCode);

        // Já autenticado: a área logada responde 200 (não redireciona para o login).
        var area = await client.GetAsync(registro.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, area.StatusCode);
    }

    [Fact]
    public async Task Registro_rejeita_login_ja_existente()
    {
        var client = CriarClient();
        var pagina = await client.GetAsync("/Account/Register");
        var token = HtmlHelpers.ExtractAntiForgeryToken(await pagina.Content.ReadAsStringAsync());

        var registro = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Nome"] = "Outra Ana",
                ["Login"] = "ana", // já existe (seed)
                ["Senha"] = "Senha@123",
                ["ConfirmarSenha"] = "Senha@123",
                ["__RequestVerificationToken"] = token,
            }));

        Assert.Equal(HttpStatusCode.OK, registro.StatusCode); // volta com erro
        Assert.Contains("Já existe um usuário", await registro.Content.ReadAsStringAsync());
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
