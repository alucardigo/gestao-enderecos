using System.Globalization;
using System.Text;
using CsvHelper;
using GestaoEnderecos.Models;

namespace GestaoEnderecos.Services;

/// <summary>
/// Gera o CSV de endereços. Usa CsvHelper (escaping RFC 4180 resolvido) e grava em UTF-8 com BOM
/// para que o Excel em pt-BR abra os acentos corretamente.
/// </summary>
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

    private static string FormatarCep(string? cep) =>
        cep is { Length: 8 } ? $"{cep[..5]}-{cep[5..]}" : cep ?? string.Empty;
}
