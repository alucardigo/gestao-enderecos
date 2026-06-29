using System.ComponentModel.DataAnnotations;

namespace GestaoEnderecos.ViewModels;

/// <summary>Criação de usuário pela área administrativa.</summary>
public class UsuarioCreateViewModel
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(120)]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o usuário.")]
    [StringLength(60, MinimumLength = 3, ErrorMessage = "O usuário deve ter entre 3 e 60 caracteres.")]
    [Display(Name = "Usuário")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(100)]
    [SenhaForte]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    [Display(Name = "Administrador")]
    public bool IsAdmin { get; set; }
}

/// <summary>Edição de usuário pela área administrativa (sem senha).</summary>
public class UsuarioEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(120)]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o usuário.")]
    [StringLength(60, MinimumLength = 3, ErrorMessage = "O usuário deve ter entre 3 e 60 caracteres.")]
    [Display(Name = "Usuário")]
    public string Login { get; set; } = string.Empty;

    [Display(Name = "Administrador")]
    public bool IsAdmin { get; set; }
}

/// <summary>Redefinição de senha de um usuário pela área administrativa.</summary>
public class RedefinirSenhaViewModel
{
    public int Id { get; set; }

    [Display(Name = "Usuário")]
    public string NomeUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [StringLength(100)]
    [SenhaForte]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string NovaSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}
