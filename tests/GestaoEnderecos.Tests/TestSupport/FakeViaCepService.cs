using GestaoEnderecos.Services;

namespace GestaoEnderecos.Tests.TestSupport;

/// <summary>ViaCEP falso para testes de integração — sem rede. Conhece um único CEP.</summary>
public sealed class FakeViaCepService : IViaCepService
{
    public Task<EnderecoViaCep?> BuscarAsync(string cep, CancellationToken ct = default)
    {
        var digitos = new string([.. cep.Where(char.IsDigit)]);
        EnderecoViaCep? resultado = digitos == "01001000"
            ? new EnderecoViaCep("01001000", "Praça da Sé", "Sé", "São Paulo", "SP", "lado ímpar")
            : null;
        return Task.FromResult(resultado);
    }
}
