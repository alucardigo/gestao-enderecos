using System.ComponentModel.DataAnnotations;
using GestaoEnderecos.Models;

namespace GestaoEnderecos.ViewModels;

/// <summary>Valida que o valor é uma das 27 UFs brasileiras (compartilhada por form e importação).</summary>
public sealed class UfValidaAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is string s && UnidadesFederativas.EhValida(s);
}
