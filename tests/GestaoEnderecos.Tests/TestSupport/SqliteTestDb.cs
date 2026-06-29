using GestaoEnderecos.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GestaoEnderecos.Tests.TestSupport;

/// <summary>
/// Banco SQLite in-memory para testes — fiel à tradução LINQ real (ao contrário do provider
/// EF InMemory). A conexão é mantida aberta enquanto a instância viver. Permite obter contextos
/// como usuários diferentes, para exercitar o filtro global por usuário.
/// </summary>
public sealed class SqliteTestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDb(int currentUserId = 0)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var setup = NewContext(0);
        setup.Database.EnsureCreated();
        Context = NewContext(currentUserId);
    }

    /// <summary>Contexto padrão, no usuário informado no construtor.</summary>
    public AppDbContext Context { get; }

    /// <summary>Cria um novo contexto enxergando os dados como <paramref name="currentUserId"/>.</summary>
    public AppDbContext NewContext(int currentUserId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new AppDbContext(options, new FakeCurrentUser(currentUserId));
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
