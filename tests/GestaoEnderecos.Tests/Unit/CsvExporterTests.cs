using System.Text;
using GestaoEnderecos.Models;
using GestaoEnderecos.Services;

namespace GestaoEnderecos.Tests.Unit;

/// <summary>T4 — prova o resultado do CSV: cabeçalho, escaping (RFC 4180), campo vazio e BOM.</summary>
public class CsvExporterTests
{
    private static readonly CsvExporter Exporter = new();

    private static string Texto(byte[] bytes)
    {
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, true);
        return reader.ReadToEnd();
    }

    private static string[] Linhas(byte[] bytes) =>
        Texto(bytes).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    private static Endereco Base() => new()
    {
        Cep = "01001000",
        Logradouro = "Praça da Sé",
        Bairro = "Sé",
        Cidade = "São Paulo",
        Uf = "SP",
        Numero = "100",
    };

    [Fact]
    public void Gera_arquivo_com_BOM_UTF8()
    {
        var bytes = Exporter.Exportar([Base()]);

        Assert.True(bytes.Length >= 3);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
    }

    [Fact]
    public void Primeira_linha_e_o_cabecalho()
    {
        var linhas = Linhas(Exporter.Exportar([]));

        Assert.Equal("CEP,Logradouro,Número,Complemento,Bairro,Cidade,UF", linhas[0]);
    }

    [Fact]
    public void Cep_sai_formatado_com_hifen()
    {
        var linhas = Linhas(Exporter.Exportar([Base()]));

        Assert.StartsWith("01001-000,", linhas[1]);
    }

    [Fact]
    public void Campo_com_virgula_e_envolvido_em_aspas()
    {
        var e = Base();
        e.Logradouro = "Rua das Flores, 23";

        var linhas = Linhas(Exporter.Exportar([e]));

        Assert.Contains("\"Rua das Flores, 23\"", linhas[1]);
    }

    [Fact]
    public void Aspas_internas_sao_duplicadas()
    {
        var e = Base();
        e.Complemento = "Bloco \"A\"";

        var linhas = Linhas(Exporter.Exportar([e]));

        Assert.Contains("\"Bloco \"\"A\"\"\"", linhas[1]);
    }

    [Fact]
    public void Campo_com_quebra_de_linha_e_envolvido_em_aspas()
    {
        var e = Base();
        e.Logradouro = "Linha 1\nLinha 2";

        var texto = Texto(Exporter.Exportar([e]));

        Assert.Contains("\"Linha 1\nLinha 2\"", texto);
    }

    [Fact]
    public void Complemento_vazio_sai_como_campo_vazio_nao_a_palavra_null()
    {
        var e = Base();
        e.Complemento = null;

        var linhas = Linhas(Exporter.Exportar([e]));

        // CEP,Logradouro,Número,Complemento(vazio),Bairro,...
        Assert.DoesNotContain("null", linhas[1]);
        Assert.Contains("100,,Sé", linhas[1]); // Número, (Complemento vazio), Bairro
    }
}
