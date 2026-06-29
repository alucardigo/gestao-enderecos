using System.ComponentModel.DataAnnotations;

namespace GestaoEnderecos.ViewModels;

/// <summary>
/// Política de senha: mínimo de 8 caracteres e pelo menos 3 das 4 classes
/// (minúsculas, maiúsculas, dígitos, símbolos). Compartilhada por cadastro, troca e admin.
/// </summary>
public sealed class SenhaForteAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string senha || senha.Length < 8)
        {
            return false;
        }

        var classes = 0;
        if (senha.Any(char.IsLower)) classes++;
        if (senha.Any(char.IsUpper)) classes++;
        if (senha.Any(char.IsDigit)) classes++;
        if (senha.Any(c => !char.IsLetterOrDigit(c))) classes++;
        return classes >= 3;
    }
}
