namespace GestaoEnderecos.Services;

/// <summary>Dados de endereço retornados pela consulta de CEP (já mapeados para o domínio).</summary>
public sealed record EnderecoViaCep(
    string Cep,
    string Logradouro,
    string Bairro,
    string Cidade,
    string Uf,
    string? Complemento);

/// <summary>
/// Consulta de endereço por CEP. É a única dependência externa atrás de interface — para poder
/// ser mockada nos testes e trocada de provedor sem tocar o resto da aplicação.
/// </summary>
public interface IViaCepService
{
    /// <returns>O endereço, ou <c>null</c> quando o CEP é inválido, não existe ou a API falha.</returns>
    Task<EnderecoViaCep?> BuscarAsync(string cep, CancellationToken ct = default);
}
