using GestaoEnderecos.Data;
using GestaoEnderecos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestaoEnderecos.Services;

/// <summary>
/// Regras de autenticação: validação de credenciais com verificação de hash em tempo
/// constante (via <see cref="IPasswordHasher{TUser}"/>, PBKDF2 do framework).
/// </summary>
public class AutenticacaoService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Usuario> _hasher;

    public AutenticacaoService(AppDbContext db, IPasswordHasher<Usuario> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    /// <summary>
    /// Valida login e senha. Retorna o usuário quando as credenciais conferem; caso contrário,
    /// <c>null</c> — sem distinguir "usuário inexistente" de "senha errada".
    /// </summary>
    public async Task<Usuario?> ValidarCredenciaisAsync(
        string login, string senha, CancellationToken ct = default)
    {
        var loginNormalizado = (login ?? string.Empty).Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Login == loginNormalizado, ct);

        if (usuario is null)
        {
            return null;
        }

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha ?? string.Empty);
        return resultado != PasswordVerificationResult.Failed ? usuario : null;
    }
}
