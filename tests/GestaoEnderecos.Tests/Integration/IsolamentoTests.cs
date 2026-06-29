using GestaoEnderecos.Models;
using GestaoEnderecos.Services;
using GestaoEnderecos.Tests.TestSupport;

namespace GestaoEnderecos.Tests.Integration;

/// <summary>
/// T7/T8 — isolamento de dados por usuário (o coração de segurança da solução). Prova que o
/// filtro global torna o vazamento impossível tanto na leitura quanto na escrita (defesa contra IDOR).
/// </summary>
public class IsolamentoTests
{
    private static EnderecoService Servico(SqliteTestDb db, int userId) =>
        new(db.NewContext(userId), new FakeCurrentUser(userId));

    private static Endereco Exemplo() => new()
    {
        Cep = "01001000",
        Logradouro = "Praça da Sé",
        Bairro = "Sé",
        Cidade = "São Paulo",
        Uf = "SP",
        Numero = "100",
    };

    private static async Task<(int Ana, int Bruno)> SeedUsuariosAsync(SqliteTestDb db)
    {
        await using var ctx = db.NewContext(0);
        var ana = new Usuario { Nome = "Ana", Login = "ana", SenhaHash = "x" };
        var bruno = new Usuario { Nome = "Bruno", Login = "bruno", SenhaHash = "x" };
        ctx.Usuarios.AddRange(ana, bruno);
        await ctx.SaveChangesAsync();
        return (ana.Id, bruno.Id);
    }

    [Fact]
    public async Task Usuario_so_enxerga_os_proprios_enderecos()
    {
        using var db = new SqliteTestDb();
        var (ana, bruno) = await SeedUsuariosAsync(db);
        var criado = await Servico(db, ana).CriarAsync(Exemplo());

        var listaAna = await Servico(db, ana).ListarAsync();
        var listaBruno = await Servico(db, bruno).ListarAsync();

        Assert.Single(listaAna);
        Assert.Equal(criado.Id, listaAna[0].Id);
        Assert.Empty(listaBruno);
    }

    [Fact]
    public async Task Usuario_nao_obtem_endereco_de_outro_por_id()
    {
        using var db = new SqliteTestDb();
        var (ana, bruno) = await SeedUsuariosAsync(db);
        var deAna = await Servico(db, ana).CriarAsync(Exemplo());

        var tentativa = await Servico(db, bruno).ObterAsync(deAna.Id);

        Assert.Null(tentativa);
    }

    [Fact]
    public async Task Usuario_nao_edita_nem_exclui_endereco_de_outro()
    {
        using var db = new SqliteTestDb();
        var (ana, bruno) = await SeedUsuariosAsync(db);
        var deAna = await Servico(db, ana).CriarAsync(Exemplo());

        var editou = await Servico(db, bruno).AtualizarAsync(deAna.Id, Exemplo());
        var excluiu = await Servico(db, bruno).ExcluirAsync(deAna.Id);

        Assert.False(editou);
        Assert.False(excluiu);
        Assert.NotNull(await Servico(db, ana).ObterAsync(deAna.Id));
    }
}
