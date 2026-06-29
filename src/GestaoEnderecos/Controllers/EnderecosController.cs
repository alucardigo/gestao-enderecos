using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEnderecos.Controllers;

/// <summary>
/// Área logada de endereços. Expandida na fatia de CRUD; por ora apenas a listagem
/// protegida, que comprova a autenticação por cookie e o redirecionamento.
/// </summary>
[Authorize]
public class EnderecosController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
