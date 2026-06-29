using GestaoEnderecos.ViewModels;

namespace GestaoEnderecos.Tests.Unit;

/// <summary>Política de senha: mínimo 8 caracteres e ao menos 3 das 4 classes.</summary>
public class SenhaForteAttributeTests
{
    private static bool Forte(string senha) => new SenhaForteAttribute().IsValid(senha);

    [Theory]
    [InlineData("Senha@123", true)]   // maiúscula + minúscula + dígito + símbolo
    [InlineData("Abcd1234", true)]    // maiúscula + minúscula + dígito (3 classes)
    [InlineData("abc12", false)]      // curta demais
    [InlineData("12345678", false)]   // só dígitos
    [InlineData("aaaaaaaa", false)]   // só minúsculas
    [InlineData("Abcdefgh", false)]   // só 2 classes (maiúscula + minúscula)
    public void Avalia_a_forca_da_senha(string senha, bool esperado) =>
        Assert.Equal(esperado, Forte(senha));
}
