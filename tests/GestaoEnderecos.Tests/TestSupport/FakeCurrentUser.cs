using GestaoEnderecos.Data;

namespace GestaoEnderecos.Tests.TestSupport;

/// <summary>Implementação de <see cref="ICurrentUser"/> para testes — Id fixo.</summary>
public sealed class FakeCurrentUser(int id) : ICurrentUser
{
    public int Id { get; } = id;
}
