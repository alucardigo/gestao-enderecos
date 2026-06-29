using GestaoEnderecos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestaoEnderecos.Data;

/// <summary>
/// Popula dois usuários de demonstração com endereços distintos. Dois usuários permitem
/// ao avaliador comprovar o isolamento de dados sem precisar rodar os testes. Idempotente.
/// </summary>
public static class DbSeeder
{
    public const string SenhaDemo = "Senha@123";

    public static async Task SeedAsync(
        AppDbContext db, IPasswordHasher<Usuario> hasher, CancellationToken ct = default)
    {
        if (await db.Usuarios.AnyAsync(ct))
        {
            return;
        }

        var ana = new Usuario { Nome = "Ana Souza", Login = "ana" };
        ana.SenhaHash = hasher.HashPassword(ana, SenhaDemo);

        var bruno = new Usuario { Nome = "Bruno Lima", Login = "bruno" };
        bruno.SenhaHash = hasher.HashPassword(bruno, SenhaDemo);

        db.Usuarios.AddRange(ana, bruno);
        await db.SaveChangesAsync(ct);

        db.Enderecos.AddRange(
            new Endereco
            {
                Cep = "01001000",
                Logradouro = "Praça da Sé",
                Bairro = "Sé",
                Cidade = "São Paulo",
                Uf = "SP",
                Numero = "100",
                IdUsuario = ana.Id,
            },
            new Endereco
            {
                Cep = "20040002",
                Logradouro = "Rua da Assembleia",
                Complemento = "Sala 2",
                Bairro = "Centro",
                Cidade = "Rio de Janeiro",
                Uf = "RJ",
                Numero = "50",
                IdUsuario = bruno.Id,
            });
        await db.SaveChangesAsync(ct);
    }
}
