namespace GestaoEnderecos.Data;

/// <summary>
/// Fornece o Id do usuário autenticado para o filtro global de consulta do
/// <see cref="AppDbContext"/>. Retorna 0 quando não há usuário (ex.: requisição anônima,
/// seed ou migração) — nesse caso o filtro não enxerga nenhum endereço.
/// </summary>
public interface ICurrentUser
{
    int Id { get; }
}
