using System.ComponentModel.DataAnnotations;

namespace GestaoEnderecos.ViewModels;

/// <summary>Edição do próprio perfil.</summary>
public class PerfilViewModel
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(120)]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "Usuário")]
    public string Login { get; set; } = string.Empty;
}

/// <summary>Troca de senha pelo próprio usuário (exige a senha atual).</summary>
public class AlterarSenhaViewModel
{
    [Required(ErrorMessage = "Informe a senha atual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha atual")]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [StringLength(100)]
    [SenhaForte(ErrorMessage = "A senha deve ter ao menos 8 caracteres, com 3 dos 4 tipos: maiúsculas, minúsculas, números e símbolos.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string NovaSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}
