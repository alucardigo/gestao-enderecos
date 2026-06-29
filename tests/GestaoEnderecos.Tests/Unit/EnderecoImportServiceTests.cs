using System.Text;
using GestaoEnderecos.Models;
using GestaoEnderecos.Services;
using GestaoEnderecos.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace GestaoEnderecos.Tests.Unit;

/// <summary>
/// Importação de CSV — caminhos felizes E infelizes: validação por linha, normalização,
/// parsing tolerante, isolamento por usuário e carga (1000 linhas).
/// </summary>
public class EnderecoImportServiceTests
{
    private const string H = "CEP,Logradouro,Número,Complemento,Bairro,Cidade,UF";

    private static async Task<(SqliteTestDb Db, int Uid)> BancoComUsuarioAsync()
    {
        var db = new SqliteTestDb();
        await using var ctx = db.NewContext(0);
        var u = new Usuario { Nome = "Ana", Login = "ana", SenhaHash = "x" };
        ctx.Usuarios.Add(u);
        await ctx.SaveChangesAsync();
        return (db, u.Id);
    }

    private static EnderecoImportService Servico(SqliteTestDb db, int uid) =>
        new(db.NewContext(uid), new FakeCurrentUser(uid));

    private static Stream Csv(string conteudo) => new MemoryStream(Encoding.UTF8.GetBytes(conteudo));

    [Fact]
    public async Task Linha_valida_e_importada()
    {
        var (db, uid) = await BancoComUsuarioAsync();
        using var _ = db;

        var r = await Servico(db, uid).ImportarAsync(Csv($"{H}\n01001-000,Praça da Sé,100,,Sé,São Paulo,SP"));

        Assert.Equal(1, r.TotalLinhas);
        Assert.Equal(1, r.Importados);
        Assert.Equal(0, r.Rejeitados);
        var salvo = await db.NewContext(uid).Enderecos.SingleAsync();
        Assert.Equal("01001000", salvo.Cep); // normalizado
        Assert.Equal("SP", salvo.Uf);
    }

    [Theory]
    [InlineData("abcdefgh", "CEP")]
    [InlineData("123", "CEP")]
    public async Task Cep_invalido_e_rejeitado(string cep, string motivo)
    {
        var (db, uid) = await BancoComUsuarioAsync();
        using var _ = db;

        var r = await Servico(db, uid).ImportarAsync(Csv($"{H}\n{cep},Rua A,10,,Centro,Cidade,SP"));

        Assert.Equal(0, r.Importados);
        Assert.Equal(1, r.Rejeitados);
        Assert.Contains(r.Erros, e => e.Mensagem.Contains(motivo));
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("Brasil")]
    [InlineData("S")]
    public async Task Uf_invalida_e_rejeitada(string uf)
    {
        var (db, uid) = await BancoComUsuarioAsync();
        using var _ = db;

        var r = await Servico(db, uid).ImportarAsync(Csv($"{H}\n01001000,Rua A,10,,Centro,Cidade,{uf}"));

        Assert.Equal(0, r.Importados);
        Assert.Contains(r.Erros, e => e.Mensagem.Contains("UF"));
    }

    [Fact]
    public async Task Campos_obrigatorios_faltando_sao_rejeitados()
    {
        var (db, uid) = await BancoComUsuarioAsync();
        using var _ = db;

        // Sem logradouro, sem bairro, sem cidade, sem número.
        var r = await Servico(db, uid).ImportarAsync(Csv($"{H}\n01001000,,, ,,,SP"));

        Assert.Equal(0, r.Importados);
        Assert.Equal(1, r.Rejeitados);
        Assert.Contains(r.Erros, e => e.Mensagem.Contains("Logradouro"));
        Assert.Contains(r.Erros, e => e.Mensagem.Contains("Bairro"));
        Assert.Contains(r.Erros, e => e.Mensagem.Contains("Cidade"));
        Assert.Contains(r.Erros, e => e.Mensagem.Contains("Número"));
    }

    [Fact]
    public async Task Mistura_de_validos_e_invalidos_conta_certo()
    {
        var (db, uid) = await BancoComUsuarioAsync();
        using var _ = db;

        var csv = string.Join("\n",
            H,
            "01001000,Rua Boa,10,,Centro,São Paulo,SP",   // válida
            "abc,Rua Ruim,20,,Centro,São Paulo,SP",       // CEP inválido
            "20040002,Rua Boa 2,30,Sala 5,Centro,Rio,RJ", // válida
            "01001000,Rua Ruim 2,40,,Centro,Cidade,ZZ",   // UF inválida
            "30140071,Rua Boa 3,50,,Centro,BH,MG");       // válida

        var r = await Servico(db, uid).ImportarAsync(Csv(csv));

        Assert.Equal(5, r.TotalLinhas);
        Assert.Equal(3, r.Importados);
        Assert.Equal(2, r.Rejeitados);
        Assert.Equal(3, await db.NewContext(uid).Enderecos.CountAsync());
    }

    [Fact]
    public async Task Cep_com_mascara_e_uf_minuscula_sao_normalizados()
    {
        var (db, uid) = await BancoComUsuarioAsync();
        using var _ = db;

        var r = await Servico(db, uid).ImportarAsync(Csv($"{H}\n01.001-000,Rua A,10,,Centro,Cidade,sp"));

        Assert.Equal(1, r.Importados);
        var salvo = await db.NewContext(uid).Enderecos.SingleAsync();
        Assert.Equal("01001000", salvo.Cep);
        Assert.Equal("SP", salvo.Uf);
    }

    [Fact]
    public async Task Campo_com_virgula_entre_aspas_e_lido_corretamente()
    {
        var (db, uid) = await BancoComUsuarioAsync();
        using var _ = db;

        var r = await Servico(db, uid).ImportarAsync(Csv($"{H}\n01001000,\"Rua das Flores, 23\",10,,Centro,Cidade,SP"));

        Assert.Equal(1, r.Importados);
        var salvo = await db.NewContext(uid).Enderecos.SingleAsync();
        Assert.Equal("Rua das Flores, 23", salvo.Logradouro);
    }

    [Fact]
    public async Task Cabecalho_sem_acento_tambem_funciona()
    {
        var (db, uid) = await BancoComUsuarioAsync();
        using var _ = db;

        var r = await Servico(db, uid).ImportarAsync(
            Csv("CEP,Logradouro,Numero,Complemento,Bairro,Cidade,UF\n01001000,Rua A,10,,Centro,Cidade,SP"));

        Assert.Equal(1, r.Importados);
    }

    [Fact]
    public async Task Arquivo_so_com_cabecalho_nao_importa_nada()
    {
        var (db, uid) = await BancoComUsuarioAsync();
        using var _ = db;

        var r = await Servico(db, uid).ImportarAsync(Csv(H));

        Assert.Equal(0, r.TotalLinhas);
        Assert.Equal(0, r.Importados);
    }

    [Fact]
    public async Task Enderecos_importados_pertencem_ao_usuario_atual()
    {
        var (db, uid) = await BancoComUsuarioAsync();
        using var _ = db;

        await Servico(db, uid).ImportarAsync(Csv($"{H}\n01001000,Rua A,10,,Centro,Cidade,SP"));

        // Outro usuário (id diferente) não enxerga nada (filtro global).
        Assert.Empty(await db.NewContext(uid + 999).Enderecos.ToListAsync());
        Assert.Single(await db.NewContext(uid).Enderecos.ToListAsync());
    }

    [Fact]
    public async Task Importacao_massiva_de_1000_linhas()
    {
        var (db, uid) = await BancoComUsuarioAsync();
        using var _ = db;

        var sb = new StringBuilder().AppendLine(H);
        var esperadosValidos = 0;
        for (var i = 0; i < 1000; i++)
        {
            if (i % 5 == 0)
            {
                sb.AppendLine($"00000000,Rua {i},{i},,Centro,Cidade,ZZ"); // UF inválida
            }
            else
            {
                sb.AppendLine($"01001000,Rua {i},{i},,Centro,Cidade,SP"); // válida
                esperadosValidos++;
            }
        }

        var r = await Servico(db, uid).ImportarAsync(Csv(sb.ToString()));

        Assert.Equal(1000, r.TotalLinhas);
        Assert.Equal(esperadosValidos, r.Importados); // 800
        Assert.Equal(1000 - esperadosValidos, r.Rejeitados); // 200
        Assert.Equal(esperadosValidos, await db.NewContext(uid).Enderecos.CountAsync());
    }
}
