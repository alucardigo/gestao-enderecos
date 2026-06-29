using System.Globalization;
using System.Text;
using CsvHelper;
using GestaoEnderecos.Models;

namespace GestaoEnderecos.Services;

/// <summary>
/// Gera o CSV de endereços. Usa CsvHelper (escaping RFC 4180 resolvido) e grava em UTF-8 com BOM
/// para que o Excel em pt-BR abra os acentos corretamente.
/// </summary>
/// <remarks>
/// Decisão consciente: NÃO aplicamos defesa contra "CSV formula injection" (prefixar = + - @).
/// O arquivo contém os próprios endereços do usuário e é aberto por ele — o autor do dado é o
/// mesmo que o consome —, então o vetor praticamente não se aplica; e prefixar corromperia dados
/// legítimos (ex.: um complemento "=A" viraria "'=A" visível no Excel). Em um cenário onde o CSV
/// fosse consumido por terceiros, habilitar InjectionOptions.Escape do CsvHelper seria o caminho.
/// </remarks>
public sealed class CsvExporter
{
    private static readonly string[] Cabecalho =
        ["CEP", "Logradouro", "Número", "Complemento", "Bairro", "Cidade", "UF"];

    public byte[] Exportar(IEnumerable<Endereco> enderecos)
    {
        using var memoria = new MemoryStream();

        // BOM UTF-8 (encoderShouldEmitUTF8Identifier: true).
        using (var writer = new StreamWriter(memoria, new UTF8Encoding(true)))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var coluna in Cabecalho)
            {
                csv.WriteField(coluna);
            }

            csv.NextRecord();

            foreach (var e in enderecos)
            {
                csv.WriteField(FormatarCep(e.Cep));
                csv.WriteField(e.Logradouro);
                csv.WriteField(e.Numero);
                csv.WriteField(e.Complemento ?? string.Empty); // vazio é campo vazio, nunca "null"
                csv.WriteField(e.Bairro);
                csv.WriteField(e.Cidade);
                csv.WriteField(e.Uf);
                csv.NextRecord();
            }
        }

        return memoria.ToArray();
    }

    /// <summary>Modelo de planilha para importação: mesmo cabeçalho do export + 2 linhas de exemplo.</summary>
    public byte[] GerarModeloImportacao() =>
        Exportar(
        [
            new Endereco
            {
                Cep = "01001000", Logradouro = "Praça da Sé", Numero = "100",
                Complemento = "", Bairro = "Sé", Cidade = "São Paulo", Uf = "SP",
            },
            new Endereco
            {
                Cep = "20040002", Logradouro = "Rua da Assembleia", Numero = "50",
                Complemento = "Sala 2", Bairro = "Centro", Cidade = "Rio de Janeiro", Uf = "RJ",
            },
        ]);

    private static string FormatarCep(string? cep) =>
        cep is { Length: 8 } ? $"{cep[..5]}-{cep[5..]}" : cep ?? string.Empty;
}
