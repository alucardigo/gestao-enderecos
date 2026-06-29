using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace GestaoEnderecos.Services;

/// <summary>
/// Implementação do <see cref="IViaCepService"/> sobre um <c>HttpClient</c> tipado
/// (gerenciado pelo <c>IHttpClientFactory</c>, com timeout configurado). Nunca lança para o
/// chamador: em qualquer falha, devolve <c>null</c> e a UI cai para o preenchimento manual.
/// </summary>
public sealed class ViaCepService : IViaCepService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ViaCepService> _logger;

    public ViaCepService(HttpClient http, IMemoryCache cache, ILogger<ViaCepService> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<EnderecoViaCep?> BuscarAsync(string cep, CancellationToken ct = default)
    {
        var digitos = EnderecoService.NormalizarCep(cep);
        if (digitos.Length != 8)
        {
            return null;
        }

        // Cache: o CEP é um dado estável; evita repetir a ida à API externa sob carga.
        if (_cache.TryGetValue($"cep:{digitos}", out EnderecoViaCep? cacheado))
        {
            return cacheado;
        }

        try
        {
            var dto = await _http.GetFromJsonAsync<ViaCepResponse>($"ws/{digitos}/json/", ct);

            // CEP inexistente retorna HTTP 200 com {"erro":"true"} — checar ANTES de mapear.
            if (dto is null || dto.Erro)
            {
                return null;
            }

            var resultado = new EnderecoViaCep(
                Cep: digitos,
                Logradouro: dto.Logradouro ?? string.Empty,
                Bairro: dto.Bairro ?? string.Empty,
                Cidade: dto.Localidade ?? string.Empty,
                Uf: dto.Uf ?? string.Empty,
                Complemento: dto.Complemento);

            _cache.Set($"cep:{digitos}", resultado, TimeSpan.FromHours(1));
            return resultado;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Timeout, indisponibilidade ou JSON inesperado: degrada graciosamente.
            _logger.LogWarning(ex, "Falha ao consultar o ViaCEP para o CEP {Cep}.", digitos);
            return null;
        }
    }
}
