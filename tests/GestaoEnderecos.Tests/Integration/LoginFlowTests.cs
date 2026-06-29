using System.Net;
using GestaoEnderecos.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GestaoEnderecos.Tests.Integration;

/// <summary>T9 — fluxo de autenticação ponta a ponta sobre a aplicação real.</summary>
public class LoginFlowTests : IClassFixture<GestaoWebAppFactory>
{
    private readonly GestaoWebAppFactory _factory;

    public LoginFlowTests(GestaoWebAppFactory factory)
    {
        _factory = factory;
        _factory.Seed();
    }

    private HttpClient CriarClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Login_valido_redireciona_para_a_lista_de_enderecos()
    {
        var client = CriarClient();

        var pagina = await client.GetAsync("/Account/Login");
        pagina.EnsureSuccessStatusCode();
        var token = HtmlHelpers.ExtractAntiForgeryToken(await pagina.Content.ReadAsStringAsync());

        var resposta = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Login"] = "ana",
                ["Senha"] = DbSeederSenha,
                ["__RequestVerificationToken"] = token,
            }));

        // Login OK redireciona para a área logada (a rota padrão colapsa Enderecos/Index em "/").
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        var destino = resposta.Headers.Location!.OriginalString;
        Assert.DoesNotContain("/Account/Login", destino);

        // Seguindo o redirecionamento autenticado (o cookie de auth foi emitido no POST).
        var areaLogada = await client.GetAsync(destino);
        Assert.Equal(HttpStatusCode.OK, areaLogada.StatusCode);
        Assert.Contains("Seus endereços", await areaLogada.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_invalido_retorna_a_pagina_com_erro()
    {
        var client = CriarClient();

        var pagina = await client.GetAsync("/Account/Login");
        var token = HtmlHelpers.ExtractAntiForgeryToken(await pagina.Content.ReadAsStringAsync());

        var resposta = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Login"] = "ana",
                ["Senha"] = "senha-errada",
                ["__RequestVerificationToken"] = token,
            }));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Credenciais inválidas", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Area_logada_redireciona_anonimo_para_o_login()
    {
        var client = CriarClient();

        var resposta = await client.GetAsync("/Enderecos");

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Account/Login", resposta.Headers.Location!.OriginalString);
    }

    private const string DbSeederSenha = "Senha@123";
}
