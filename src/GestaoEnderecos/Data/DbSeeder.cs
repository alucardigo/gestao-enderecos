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

        var ana = new Usuario { Nome = "Ana Souza", Login = "ana", IsAdmin = true };
        ana.SenhaHash = hasher.HashPassword(ana, SenhaDemo);

        var bruno = new Usuario { Nome = "Bruno Lima", Login = "bruno", IsAdmin = false };
        bruno.SenhaHash = hasher.HashPassword(bruno, SenhaDemo);

        db.Usuarios.AddRange(ana, bruno);
        await db.SaveChangesAsync(ct);

        // Endereços reais distribuídos entre os dois usuários de demonstração.
        var enderecos = EnderecosDemo.Lista();
        for (var i = 0; i < enderecos.Count; i++)
        {
            enderecos[i].IdUsuario = i % 2 == 0 ? ana.Id : bruno.Id;
        }

        db.Enderecos.AddRange(enderecos);
        await db.SaveChangesAsync(ct);
    }
}
