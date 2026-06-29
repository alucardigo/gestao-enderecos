using System.Security.Claims;

namespace GestaoEnderecos.Data;

/// <summary>
/// Lê o Id do usuário a partir da claim do cookie de autenticação. É registrado como
/// <c>Scoped</c> — uma instância por requisição — para que o filtro global capture o
/// usuário correto a cada requisição.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    public CurrentUser(IHttpContextAccessor accessor)
    {
        var valor = accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Id = int.TryParse(valor, out var id) ? id : 0;
    }

    public int Id { get; }
}
