namespace GestaoEnderecos.Models;

/// <summary>
/// Usuário do sistema. A senha nunca é armazenada em texto puro — apenas o hash
/// versionado gerado pelo <c>PasswordHasher</c> (ver <see cref="Services.AutenticacaoService"/>).
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    /// <summary>Login do usuário (mapeado para a coluna "Usuario"). Único no sistema.</summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>Hash versionado da senha (coluna "SenhaHash").</summary>
    public string SenhaHash { get; set; } = string.Empty;

    public ICollection<Endereco> Enderecos { get; set; } = new List<Endereco>();
}
