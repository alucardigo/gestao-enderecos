using GestaoEnderecos.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoEnderecos.Data;

/// <summary>
/// Contexto de persistência (EF Core, Code-First). Concentra o mapeamento Fluent API e,
/// crucialmente, o <b>filtro global por usuário</b> que torna o vazamento de dados entre
/// usuários impossível por construção.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly ICurrentUser _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Endereco> Enderecos => Set<Endereco>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasKey(u => u.Id);
            e.Property(u => u.Nome).HasMaxLength(120).IsRequired();
            e.Property(u => u.Login).HasColumnName("Usuario").HasMaxLength(60).IsRequired();
            e.Property(u => u.SenhaHash).HasMaxLength(256).IsRequired();
            // Login único: o banco é a última linha de defesa contra contas duplicadas.
            e.HasIndex(u => u.Login).IsUnique().HasDatabaseName("UX_Usuarios_Usuario");
        });

        modelBuilder.Entity<Endereco>(e =>
        {
            e.ToTable("Enderecos", t =>
            {
                // Defesa em profundidade no nível de dados (somente SQL Server; os testes usam
                // SQLite e a validação de domínio vive nas ViewModels/Services).
                if (Database.IsSqlServer())
                {
                    t.HasCheckConstraint("CK_Enderecos_Cep_Digitos", "[Cep] NOT LIKE '%[^0-9]%'");
                }
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Cep).HasColumnType("char(8)").IsRequired();
            e.Property(x => x.Logradouro).HasMaxLength(150).IsRequired();
            e.Property(x => x.Complemento).HasMaxLength(60);
            e.Property(x => x.Bairro).HasMaxLength(80).IsRequired();
            e.Property(x => x.Cidade).HasMaxLength(80).IsRequired();
            e.Property(x => x.Uf).HasColumnType("char(2)").IsRequired();
            e.Property(x => x.Numero).HasMaxLength(15).IsRequired();
            e.HasOne(x => x.Usuario)
                .WithMany(u => u.Enderecos)
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.IdUsuario).HasDatabaseName("IX_Enderecos_IdUsuario");

            // Isolamento por usuário: NENHUMA consulta (list, Find, edição, exclusão) enxerga
            // endereço de outro usuário. O filtro lê _currentUser.Id no momento da consulta; o
            // isolamento correto depende de AppDbContext e ICurrentUser serem Scoped (uma instância
            // por requisição, criada após a autenticação). Fora de requisição (seed/migração) o Id
            // é 0 e o filtro não retorna nenhum endereço — seguro, sem NullReferenceException.
            e.HasQueryFilter(x => x.IdUsuario == _currentUser.Id);
        });
    }
}
