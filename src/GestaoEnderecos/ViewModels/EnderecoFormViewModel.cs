using System.ComponentModel.DataAnnotations;

namespace GestaoEnderecos.ViewModels;

/// <summary>
/// Formulário de criação/edição de endereço. Validação de apresentação via DataAnnotations
/// (reaproveitada no cliente pelo jQuery Unobtrusive). A validação canônica é sempre no servidor.
/// </summary>
public class EnderecoFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o CEP.")]
    [RegularExpression(@"^\d{5}-?\d{3}$", ErrorMessage = "CEP deve ter 8 dígitos (ex.: 00000-000).")]
    [Display(Name = "CEP")]
    public string Cep { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o logradouro.")]
    [StringLength(150)]
    [Display(Name = "Logradouro")]
    public string Logradouro { get; set; } = string.Empty;

    [StringLength(60)]
    [Display(Name = "Complemento")]
    public string? Complemento { get; set; }

    [Required(ErrorMessage = "Informe o bairro.")]
    [StringLength(80)]
    [Display(Name = "Bairro")]
    public string Bairro { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a cidade.")]
    [StringLength(80)]
    [Display(Name = "Cidade")]
    public string Cidade { get; set; } = string.Empty;

    [Required(ErrorMessage = "UF.")]
    [RegularExpression(@"^[A-Za-z]{2}$", ErrorMessage = "UF deve ter 2 letras.")]
    [Display(Name = "UF")]
    public string Uf { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o número.")]
    [StringLength(15)]
    [Display(Name = "Número")]
    public string Numero { get; set; } = string.Empty;
}
