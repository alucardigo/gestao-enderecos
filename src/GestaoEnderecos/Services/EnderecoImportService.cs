using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using GestaoEnderecos.Data;
using GestaoEnderecos.Models;

namespace GestaoEnderecos.Services;

/// <summary>Um erro de importação, com a linha do arquivo e o motivo.</summary>
public sealed record ImportError(int Linha, string Mensagem);

/// <summary>Resultado consolidado de uma importação.</summary>
public sealed class ImportResult
{
    public int TotalLinhas { get; init; }
    public int Importados { get; init; }
    public IReadOnlyList<ImportError> Erros { get; init; } = [];

    /// <summary>Linhas distintas rejeitadas (uma linha pode acumular vários motivos).</summary>
    public int Rejeitados => Erros.Select(e => e.Linha).Distinct().Count();
}

/// <summary>Linha bruta do CSV (todos os campos como texto, para validar sem estourar).</summary>
internal sealed class ImportRow
{
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
}

internal sealed class ImportRowMap : ClassMap<ImportRow>
{
    public ImportRowMap()
    {
        Map(m => m.Cep).Name("cep");
        Map(m => m.Logradouro).Name("logradouro");
        Map(m => m.Numero).Name("número", "numero", "nº", "no");
        Map(m => m.Complemento).Name("complemento").Optional();
        Map(m => m.Bairro).Name("bairro");
        Map(m => m.Cidade).Name("cidade");
        Map(m => m.Uf).Name("uf");
    }
}

/// <summary>
/// Importa endereços de um CSV. Cada linha é validada isoladamente: as válidas são gravadas
/// (em lote) e as inválidas são reportadas com a linha e o motivo — nunca falha o arquivo inteiro
/// por causa de uma linha ruim. Os endereços importados pertencem ao usuário autenticado.
/// </summary>
public sealed class EnderecoImportService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public EnderecoImportService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ImportResult> ImportarAsync(Stream conteudo, CancellationToken ct = default)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true,
            PrepareHeaderForMatch = args => args.Header?.Trim().ToLowerInvariant() ?? string.Empty,
        };

        using var reader = new StreamReader(conteudo);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<ImportRowMap>();

        if (!await csv.ReadAsync() || !csv.ReadHeader())
        {
            return new ImportResult { TotalLinhas = 0, Importados = 0, Erros = [] };
        }

        var validos = new List<Endereco>();
        var erros = new List<ImportError>();
        var total = 0;

        while (await csv.ReadAsync())
        {
            total++;
            var linha = csv.Parser.Row;

            ImportRow row;
            try
            {
                row = csv.GetRecord<ImportRow>();
            }
            catch (Exception)
            {
                erros.Add(new ImportError(linha, "Linha mal formatada (CSV inválido)."));
                continue;
            }

            var (endereco, errosLinha) = ValidarEMapear(row);
            if (endereco is null)
            {
                erros.AddRange(errosLinha.Select(m => new ImportError(linha, m)));
                continue;
            }

            endereco.IdUsuario = _currentUser.Id;
            validos.Add(endereco);
        }

        if (validos.Count > 0)
        {
            _db.Enderecos.AddRange(validos);
            await _db.SaveChangesAsync(ct);
        }

        return new ImportResult { TotalLinhas = total, Importados = validos.Count, Erros = erros };
    }

    private static (Endereco? Endereco, List<string> Erros) ValidarEMapear(ImportRow row)
    {
        var erros = new List<string>();

        var cep = EnderecoService.NormalizarCep(row.Cep);
        if (cep.Length != 8)
        {
            erros.Add("CEP inválido (precisa de 8 dígitos).");
        }

        if (string.IsNullOrWhiteSpace(row.Logradouro))
        {
            erros.Add("Logradouro obrigatório.");
        }
        else if (row.Logradouro.Length > 150)
        {
            erros.Add("Logradouro excede 150 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(row.Bairro))
        {
            erros.Add("Bairro obrigatório.");
        }
        else if (row.Bairro.Length > 80)
        {
            erros.Add("Bairro excede 80 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(row.Cidade))
        {
            erros.Add("Cidade obrigatória.");
        }
        else if (row.Cidade.Length > 80)
        {
            erros.Add("Cidade excede 80 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(row.Numero))
        {
            erros.Add("Número obrigatório.");
        }
        else if (row.Numero.Length > 15)
        {
            erros.Add("Número excede 15 caracteres.");
        }

        if (!UnidadesFederativas.EhValida(row.Uf))
        {
            erros.Add("UF inválida.");
        }

        if ((row.Complemento?.Length ?? 0) > 60)
        {
            erros.Add("Complemento excede 60 caracteres.");
        }

        if (erros.Count > 0)
        {
            return (null, erros);
        }

        return (new Endereco
        {
            Cep = cep,
            Logradouro = row.Logradouro!.Trim(),
            Complemento = string.IsNullOrWhiteSpace(row.Complemento) ? null : row.Complemento!.Trim(),
            Bairro = row.Bairro!.Trim(),
            Cidade = row.Cidade!.Trim(),
            Uf = row.Uf!.Trim().ToUpperInvariant(),
            Numero = row.Numero!.Trim(),
        }, erros);
    }
}
