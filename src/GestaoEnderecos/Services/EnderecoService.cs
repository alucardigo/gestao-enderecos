using GestaoEnderecos.Data;
using GestaoEnderecos.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoEnderecos.Services;

/// <summary>Uma página de endereços (resultado paginado + metadados para a navegação).</summary>
public sealed record PaginaEnderecos(
    IReadOnlyList<Endereco> Itens, int Total, int Pagina, int TamanhoPagina, string? Busca)
{
    public int TotalPaginas => Total == 0 ? 1 : (int)Math.Ceiling((double)Total / TamanhoPagina);
}

/// <summary>
/// Regras de negócio do CRUD de endereços. Todas as leituras/escritas passam pelo
/// <see cref="AppDbContext"/>, cujo filtro global garante que um usuário só enxerga e altera
/// os próprios endereços — uma tentativa de acessar id alheio simplesmente "não encontra".
/// </summary>
public class EnderecoService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public EnderecoService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Lista todos os endereços do usuário (usado pela exportação).</summary>
    public async Task<List<Endereco>> ListarAsync(CancellationToken ct = default) =>
        await _db.Enderecos
            .AsNoTracking()
            .OrderBy(e => e.Cidade).ThenBy(e => e.Logradouro)
            .ToListAsync(ct);

    // Limite de candidatos avaliados na busca aproximada (mantém o custo controlado).
    private const int MaxCandidatosFuzzy = 5000;

    /// <summary>
    /// Lista paginada com busca tolerante. A busca é normalizada (ignora maiúsculas e acentos);
    /// se a busca exata por substring não retornar nada, cai para uma busca aproximada que tolera
    /// erros de digitação (Damerau-Levenshtein) — assim "RUA", "rua" e "rau" encontram "Rua".
    /// </summary>
    public async Task<PaginaEnderecos> ListarPaginadoAsync(
        string? busca, int pagina, int tamanho, CancellationToken ct = default)
    {
        if (pagina < 1) pagina = 1;
        if (tamanho < 1) tamanho = 10;

        var termo = TextoNormalizado.Normalizar(busca);
        var baseQuery = _db.Enderecos.AsNoTracking();

        // Sem busca: paginação simples.
        if (string.IsNullOrEmpty(termo))
        {
            return await PaginarAsync(baseQuery, busca, pagina, tamanho, ct);
        }

        // 1) Busca por substring no texto normalizado (case/acento-insensível).
        var filtrada = baseQuery.Where(e => e.TextoBusca.Contains(termo));
        if (await filtrada.AnyAsync(ct))
        {
            return await PaginarAsync(filtrada, busca, pagina, tamanho, ct);
        }

        // 2) Fallback aproximado (tolera erros de digitação), avaliado em memória e limitado.
        if (termo.Length < 3)
        {
            return new PaginaEnderecos([], 0, pagina, tamanho, busca);
        }

        var limiar = termo.Length <= 4 ? 1 : 2;
        var candidatos = await baseQuery
            .Select(e => new { e.Id, e.TextoBusca })
            .Take(MaxCandidatosFuzzy)
            .ToListAsync(ct);

        var idsRanqueados = candidatos
            .Select(c => new { c.Id, Dist = Similaridade.MenorDistanciaPorToken(c.TextoBusca, termo) })
            .Where(x => x.Dist <= limiar)
            .OrderBy(x => x.Dist).ThenBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();

        var total = idsRanqueados.Count;
        var idsPagina = idsRanqueados.Skip((pagina - 1) * tamanho).Take(tamanho).ToList();
        var itensPagina = await baseQuery.Where(e => idsPagina.Contains(e.Id)).ToListAsync(ct);
        var ordenados = itensPagina.OrderBy(e => idsPagina.IndexOf(e.Id)).ToList();

        return new PaginaEnderecos(ordenados, total, pagina, tamanho, busca);
    }

    private static async Task<PaginaEnderecos> PaginarAsync(
        IQueryable<Endereco> query, string? busca, int pagina, int tamanho, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var itens = await query
            .OrderBy(e => e.Cidade).ThenBy(e => e.Logradouro)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(ct);
        return new PaginaEnderecos(itens, total, pagina, tamanho, busca);
    }

    public async Task<Endereco?> ObterAsync(int id, CancellationToken ct = default) =>
        await _db.Enderecos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Endereco> CriarAsync(Endereco endereco, CancellationToken ct = default)
    {
        Normalizar(endereco);
        endereco.IdUsuario = _currentUser.Id;
        _db.Enderecos.Add(endereco);
        await _db.SaveChangesAsync(ct);
        return endereco;
    }

    /// <returns><c>false</c> se o endereço não existe ou pertence a outro usuário.</returns>
    public async Task<bool> AtualizarAsync(int id, Endereco dados, CancellationToken ct = default)
    {
        var atual = await _db.Enderecos.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (atual is null)
        {
            return false;
        }

        Normalizar(dados);
        atual.Cep = dados.Cep;
        atual.Logradouro = dados.Logradouro;
        atual.Complemento = dados.Complemento;
        atual.Bairro = dados.Bairro;
        atual.Cidade = dados.Cidade;
        atual.Uf = dados.Uf;
        atual.Numero = dados.Numero;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <returns><c>false</c> se o endereço não existe ou pertence a outro usuário.</returns>
    public async Task<bool> ExcluirAsync(int id, CancellationToken ct = default)
    {
        var atual = await _db.Enderecos.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (atual is null)
        {
            return false;
        }

        _db.Enderecos.Remove(atual);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Mantém o dado consistente: CEP só com dígitos, UF em maiúsculas.</summary>
    private static void Normalizar(Endereco endereco)
    {
        endereco.Cep = NormalizarCep(endereco.Cep);
        endereco.Uf = (endereco.Uf ?? string.Empty).Trim().ToUpperInvariant();
    }

    /// <summary>Remove máscara do CEP, deixando apenas os dígitos (ex.: "01001-000" → "01001000").</summary>
    public static string NormalizarCep(string? cep) =>
        new([.. (cep ?? string.Empty).Where(char.IsDigit)]);
}
