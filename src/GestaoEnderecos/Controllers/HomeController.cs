using System.Diagnostics;
using GestaoEnderecos.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEnderecos.Controllers;

/// <summary>Páginas de erro amigáveis (exceções não tratadas e códigos de status HTTP).</summary>
public class HomeController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    /// <summary>Página amigável para códigos de status (ex.: 404), via StatusCodePages.</summary>
    public IActionResult Status(int code)
    {
        ViewData["Codigo"] = code;
        return View();
    }
}
