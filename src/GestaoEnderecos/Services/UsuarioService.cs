using GestaoEnderecos.Data;
using GestaoEnderecos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestaoEnderecos.Services;

/// <summary>Resultado de uma operação de usuário, com mensagem de erro amigável quando falha.</summary>
public sealed record OperacaoUsuario(bool Ok, string? Erro = null, Usuario? Usuario = null);

/// <summary>
/// Regras de gestão de usuários: cadastro, edição, troca/redefinição de senha e exclusão.
/// O hash da senha sempre passa pelo <see cref="IPasswordHasher{TUser}"/> (PBKDF2).
/// </summary>
public class UsuarioService
{
    private const string LoginDuplicado = "Já existe um usuário com esse login.";

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Usuario> _hasher;

    public UsuarioService(AppDbContext db, IPasswordHasher<Usuario> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public Task<List<Usuario>> ListarAsync(CancellationToken ct = default) =>
        _db.Usuarios.AsNoTracking().OrderBy(u => u.Nome).ToListAsync(ct);

    public Task<Usuario?> ObterAsync(int id, CancellationToken ct = default) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<int> ContarAdminsAsync(CancellationToken ct = default) =>
        _db.Usuarios.CountAsync(u => u.IsAdmin, ct);

    public async Task<OperacaoUsuario> CriarAsync(
        string nome, string login, string senha, bool isAdmin, CancellationToken ct = default)
    {
        login = NormalizarLogin(login);
        if (await _db.Usuarios.AnyAsync(u => u.Login == login, ct))
        {
            return new OperacaoUsuario(false, LoginDuplicado);
        }

        var usuario = new Usuario { Nome = nome.Trim(), Login = login, IsAdmin = isAdmin };
        usuario.SenhaHash = _hasher.HashPassword(usuario, senha);
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(ct);
        return new OperacaoUsuario(true, Usuario: usuario);
    }

    public async Task<OperacaoUsuario> AtualizarAsync(
        int id, string nome, string login, bool isAdmin, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null)
        {
            return new OperacaoUsuario(false, "Usuário não encontrado.");
        }

        login = NormalizarLogin(login);
        if (await _db.Usuarios.AnyAsync(u => u.Login == login && u.Id != id, ct))
        {
            return new OperacaoUsuario(false, LoginDuplicado);
        }

        usuario.Nome = nome.Trim();
        usuario.Login = login;
        usuario.IsAdmin = isAdmin;
        await _db.SaveChangesAsync(ct);
        return new OperacaoUsuario(true, Usuario: usuario);
    }

    /// <summary>Redefinição de senha por um administrador (sem exigir a senha atual).</summary>
    public async Task<bool> RedefinirSenhaAsync(int id, string novaSenha, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null)
        {
            return false;
        }

        usuario.SenhaHash = _hasher.HashPassword(usuario, novaSenha);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Troca de senha pelo próprio usuário — exige a senha atual correta.</summary>
    public async Task<bool> AlterarSenhaAsync(
        int id, string senhaAtual, string novaSenha, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null)
        {
            return false;
        }

        if (_hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senhaAtual) == PasswordVerificationResult.Failed)
        {
            return false;
        }

        usuario.SenhaHash = _hasher.HashPassword(usuario, novaSenha);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AtualizarPerfilAsync(int id, string nome, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null)
        {
            return false;
        }

        usuario.Nome = nome.Trim();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExcluirAsync(int id, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null)
        {
            return false;
        }

        _db.Usuarios.Remove(usuario); // endereços do usuário caem em cascata (FK)
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public static string NormalizarLogin(string? login) =>
        (login ?? string.Empty).Trim().ToLowerInvariant();
}
