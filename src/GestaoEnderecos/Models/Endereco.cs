namespace GestaoEnderecos.Models;

/// <summary>
/// Endereço pertencente a um único usuário. O acesso é isolado por usuário através de um
/// filtro global de consulta (ver <see cref="Data.AppDbContext"/>).
/// </summary>
public class Endereco
{
    public int Id { get; set; }

    /// <summary>CEP normalizado: somente 8 dígitos, sem máscara.</summary>
    public string Cep { get; set; } = string.Empty;

    public string Logradouro { get; set; } = string.Empty;

    /// <summary>Complemento é opcional.</summary>
    public string? Complemento { get; set; }

    public string Bairro { get; set; } = string.Empty;

    public string Cidade { get; set; } = string.Empty;

    /// <summary>Unidade federativa (2 letras).</summary>
    public string Uf { get; set; } = string.Empty;

    /// <summary>Número é texto: aceita "S/N", "123-A", "0".</summary>
    public string Numero { get; set; } = string.Empty;

    public int IdUsuario { get; set; }

    public Usuario? Usuario { get; set; }
}
