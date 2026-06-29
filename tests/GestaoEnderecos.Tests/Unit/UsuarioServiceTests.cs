using GestaoEnderecos.Models;
using GestaoEnderecos.Services;
using GestaoEnderecos.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestaoEnderecos.Tests.Unit;

/// <summary>Gestão de usuários: unicidade de login, hashing e troca/redefinição de senha.</summary>
public class UsuarioServiceTests
{
    private static UsuarioService Servico(SqliteTestDb db) =>
        new(db.NewContext(0), new PasswordHasher<Usuario>());

    [Fact]
    public async Task Criar_armazena_hash_e_nao_a_senha_em_texto_puro()
    {
        using var db = new SqliteTestDb();
        var r = await Servico(db).CriarAsync("Ana", "Ana", "Senha@123", isAdmin: false);

        Assert.True(r.Ok);
        var u = await db.NewContext(0).Usuarios.SingleAsync();
        Assert.Equal("ana", u.Login); // normalizado para minúsculas
        Assert.NotEqual("Senha@123", u.SenhaHash);
        Assert.True(u.SenhaHash.Length > 20);
    }

    [Fact]
    public async Task Criar_rejeita_login_duplicado()
    {
        using var db = new SqliteTestDb();
        await Servico(db).CriarAsync("Ana", "ana", "Senha@123", false);

        var r = await Servico(db).CriarAsync("Outra Ana", "ANA", "Outra@123", false);

        Assert.False(r.Ok);
        Assert.Contains("login", r.Erro!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await db.NewContext(0).Usuarios.CountAsync());
    }

    [Fact]
    public async Task AlterarSenha_exige_a_senha_atual_correta()
    {
        using var db = new SqliteTestDb();
        var criado = await Servico(db).CriarAsync("Ana", "ana", "Senha@123", false);
        var id = criado.Usuario!.Id;

        Assert.False(await Servico(db).AlterarSenhaAsync(id, "errada", "Nova@123"));
        Assert.True(await Servico(db).AlterarSenhaAsync(id, "Senha@123", "Nova@123"));

        // A nova senha passa a valer.
        var auth = new AutenticacaoService(db.NewContext(0), new PasswordHasher<Usuario>());
        Assert.NotNull(await auth.ValidarCredenciaisAsync("ana", "Nova@123"));
        Assert.Null(await auth.ValidarCredenciaisAsync("ana", "Senha@123"));
    }

    [Fact]
    public async Task RedefinirSenha_troca_sem_exigir_a_atual()
    {
        using var db = new SqliteTestDb();
        var criado = await Servico(db).CriarAsync("Ana", "ana", "Senha@123", false);

        Assert.True(await Servico(db).RedefinirSenhaAsync(criado.Usuario!.Id, "Reset@123"));

        var auth = new AutenticacaoService(db.NewContext(0), new PasswordHasher<Usuario>());
        Assert.NotNull(await auth.ValidarCredenciaisAsync("ana", "Reset@123"));
    }

    [Fact]
    public async Task Atualizar_rejeita_login_ja_usado_por_outro()
    {
        using var db = new SqliteTestDb();
        await Servico(db).CriarAsync("Ana", "ana", "Senha@123", false);
        var bruno = await Servico(db).CriarAsync("Bruno", "bruno", "Senha@123", false);

        var r = await Servico(db).AtualizarAsync(bruno.Usuario!.Id, "Bruno", "ana", isAdmin: false);

        Assert.False(r.Ok);
    }

    [Fact]
    public async Task ContarAdmins_reflete_os_administradores()
    {
        using var db = new SqliteTestDb();
        await Servico(db).CriarAsync("Ana", "ana", "Senha@123", isAdmin: true);
        await Servico(db).CriarAsync("Bruno", "bruno", "Senha@123", isAdmin: false);

        Assert.Equal(1, await Servico(db).ContarAdminsAsync());
    }
}
