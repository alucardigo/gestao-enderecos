using GestaoEnderecos.Models;
using GestaoEnderecos.Services;
using GestaoEnderecos.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;

namespace GestaoEnderecos.Tests.Unit;

/// <summary>T5 — prova o uso correto do PasswordHasher (senha certa passa, errada falha).</summary>
public class AutenticacaoServiceTests
{
    private static (AutenticacaoService Service, SqliteTestDb Db) CriarComUsuario(
        string login, string senha)
    {
        var db = new SqliteTestDb();
        var hasher = new PasswordHasher<Usuario>();
        var usuario = new Usuario { Nome = "Fulano", Login = login };
        usuario.SenhaHash = hasher.HashPassword(usuario, senha);
        db.Context.Usuarios.Add(usuario);
        db.Context.SaveChanges();
        return (new AutenticacaoService(db.Context, hasher), db);
    }

    [Fact]
    public async Task Senha_correta_retorna_o_usuario()
    {
        var (service, db) = CriarComUsuario("ana", "Senha@123");
        using var _ = db;

        var resultado = await service.ValidarCredenciaisAsync("ana", "Senha@123");

        Assert.NotNull(resultado);
        Assert.Equal("ana", resultado!.Login);
    }

    [Fact]
    public async Task Senha_incorreta_retorna_null()
    {
        var (service, db) = CriarComUsuario("ana", "Senha@123");
        using var _ = db;

        var resultado = await service.ValidarCredenciaisAsync("ana", "senha-errada");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task Usuario_inexistente_retorna_null()
    {
        var (service, db) = CriarComUsuario("ana", "Senha@123");
        using var _ = db;

        var resultado = await service.ValidarCredenciaisAsync("ninguem", "Senha@123");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task Login_e_case_insensitive()
    {
        var (service, db) = CriarComUsuario("ana", "Senha@123");
        using var _ = db;

        var resultado = await service.ValidarCredenciaisAsync("ANA", "Senha@123");

        Assert.NotNull(resultado);
    }

    [Fact]
    public void Hash_armazenado_nao_e_a_senha_em_texto_puro()
    {
        var (_, db) = CriarComUsuario("ana", "Senha@123");
        using var _d = db;

        var usuario = db.Context.Usuarios.Single();

        Assert.NotEqual("Senha@123", usuario.SenhaHash);
        Assert.True(usuario.SenhaHash.Length > 20);
    }
}
