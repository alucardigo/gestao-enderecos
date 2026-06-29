using GestaoEnderecos.Models;
using GestaoEnderecos.Services;
using GestaoEnderecos.Tests.TestSupport;

namespace GestaoEnderecos.Tests.Unit;

/// <summary>T6 — normalização do CEP/UF (dado consistente, RN-03/RN-05).</summary>
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

    [Fact]
    public async Task ListarPaginado_pagina_e_filtra_por_busca()
    {
        using var db = new SqliteTestDb();
        int uid;
        await using (var ctx = db.NewContext(0))
        {
            var u = new Usuario { Nome = "Ana", Login = "ana", SenhaHash = "x" };
            ctx.Usuarios.Add(u);
            await ctx.SaveChangesAsync();
            uid = u.Id;
        }

        var criador = new EnderecoService(db.NewContext(uid), new FakeCurrentUser(uid));
        for (var i = 0; i < 25; i++)
        {
            await criador.CriarAsync(new Endereco
            {
                Cep = "01001000",
                Logradouro = $"Rua {i}",
                Bairro = "Centro",
                Cidade = i < 5 ? "Curitiba" : "São Paulo",
                Uf = "SP",
                Numero = $"{i}",
            });
        }

        var pagina1 = await new EnderecoService(db.NewContext(uid), new FakeCurrentUser(uid))
            .ListarPaginadoAsync(null, 1, 10);
        Assert.Equal(25, pagina1.Total);
        Assert.Equal(10, pagina1.Itens.Count);
        Assert.Equal(3, pagina1.TotalPaginas);

        var busca = await new EnderecoService(db.NewContext(uid), new FakeCurrentUser(uid))
            .ListarPaginadoAsync("Curitiba", 1, 10);
        Assert.Equal(5, busca.Total);
    }

    private static async Task<(SqliteTestDb Db, int Uid)> BancoComEnderecoAsync(Endereco endereco)
    {
        var db = new SqliteTestDb();
        int uid;
        await using (var ctx = db.NewContext(0))
        {
            var u = new Usuario { Nome = "Ana", Login = "ana", SenhaHash = "x" };
            ctx.Usuarios.Add(u);
            await ctx.SaveChangesAsync();
            uid = u.Id;
        }

        await new EnderecoService(db.NewContext(uid), new FakeCurrentUser(uid)).CriarAsync(endereco);
        return (db, uid);
    }

    [Theory]
    [InlineData("rua")]   // minúscula — o dado tem "Rua" (no SQLite o LIKE é sensível a caixa!)
    [InlineData("RUA")]   // maiúscula
    [InlineData("Rua")]   // exata
    [InlineData("rau")]   // erro de digitação (transposição)
    [InlineData("flores")]
    public async Task Busca_tolera_caixa_e_erros_de_digitacao(string termo)
    {
        var (db, uid) = await BancoComEnderecoAsync(new Endereco
        {
            Cep = "01001000",
            Logradouro = "Rua das Flores",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Uf = "SP",
            Numero = "10",
        });
        using var _ = db;

        var r = await new EnderecoService(db.NewContext(uid), new FakeCurrentUser(uid))
            .ListarPaginadoAsync(termo, 1, 10);

        Assert.Equal(1, r.Total);
    }

    [Theory]
    [InlineData("sao paulo")] // sem acento — o dado tem "São Paulo"
    [InlineData("SÃO")]
    [InlineData("são paulo")]
    public async Task Busca_ignora_acentos(string termo)
    {
        var (db, uid) = await BancoComEnderecoAsync(new Endereco
        {
            Cep = "01001000",
            Logradouro = "Avenida Central",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Uf = "SP",
            Numero = "10",
        });
        using var _ = db;

        var r = await new EnderecoService(db.NewContext(uid), new FakeCurrentUser(uid))
            .ListarPaginadoAsync(termo, 1, 10);

        Assert.Equal(1, r.Total);
    }

    [Fact]
    public async Task Busca_sem_correspondencia_retorna_vazio()
    {
        var (db, uid) = await BancoComEnderecoAsync(new Endereco
        {
            Cep = "01001000",
            Logradouro = "Rua das Flores",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Uf = "SP",
            Numero = "10",
        });
        using var _ = db;

        var r = await new EnderecoService(db.NewContext(uid), new FakeCurrentUser(uid))
            .ListarPaginadoAsync("xyzklmnop", 1, 10);

        Assert.Equal(0, r.Total);
    }

    [Fact]
    public async Task CriarAsync_persiste_cep_sem_mascara_e_uf_em_maiusculas()
    {
        using var db = new SqliteTestDb();
        int idUsuario;
        await using (var ctx = db.NewContext(0))
        {
            var usuario = new Usuario { Nome = "Ana", Login = "ana", SenhaHash = "x" };
            ctx.Usuarios.Add(usuario);
            await ctx.SaveChangesAsync();
            idUsuario = usuario.Id;
        }

        var criado = await new EnderecoService(db.NewContext(idUsuario), new FakeCurrentUser(idUsuario))
            .CriarAsync(new Endereco
            {
                Cep = "30140-071",
                Logradouro = "Rua da Bahia",
                Bairro = "Centro",
                Cidade = "Belo Horizonte",
                Uf = "mg",
                Numero = "1",
            });

        var relido = await new EnderecoService(db.NewContext(idUsuario), new FakeCurrentUser(idUsuario))
            .ObterAsync(criado.Id);

        Assert.NotNull(relido);
        Assert.Equal("30140071", relido!.Cep);
        Assert.Equal("MG", relido.Uf);
    }
}
