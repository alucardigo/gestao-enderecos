using System.Security.Claims;
using GestaoEnderecos.Models;
using GestaoEnderecos.Services;
using GestaoEnderecos.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestaoEnderecos.Controllers;

/// <summary>Login, cadastro, perfil e troca de senha. Controller fino: valida, delega, redireciona.</summary>
public class AccountController : Controller
{
    private readonly AutenticacaoService _auth;
    private readonly UsuarioService _usuarios;

    public AccountController(AutenticacaoService auth, UsuarioService usuarios)
    {
        _auth = auth;
        _usuarios = usuarios;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Enderecos");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usuario = await _auth.ValidarCredenciaisAsync(model.Login, model.Senha, ct);
        if (usuario is null)
        {
            ModelState.AddModelError(string.Empty, "Credenciais inválidas.");
            return View(model);
        }

        await SignInAsync(usuario, model.LembrarMe);
        return Url.IsLocalUrl(model.ReturnUrl)
            ? Redirect(model.ReturnUrl!)
            : RedirectToAction("Index", "Enderecos");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Perfil(CancellationToken ct)
    {
        var usuario = await _usuarios.ObterAsync(UsuarioAtualId, ct);
        if (usuario is null)
        {
            return NotFound();
        }

        return View(new PerfilViewModel { Nome = usuario.Nome, Login = usuario.Login });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(PerfilViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _usuarios.AtualizarPerfilAsync(UsuarioAtualId, model.Nome, ct);

        // Atualiza a claim de nome para o menu refletir o novo nome imediatamente.
        var usuario = await _usuarios.ObterAsync(UsuarioAtualId, ct);
        if (usuario is not null)
        {
            await SignInAsync(usuario, isPersistent: false);
        }

        TempData["Sucesso"] = "Perfil atualizado.";
        return RedirectToAction(nameof(Perfil));
    }

    [Authorize]
    [HttpGet]
    public IActionResult AlterarSenha() => View(new AlterarSenhaViewModel());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ok = await _usuarios.AlterarSenhaAsync(UsuarioAtualId, model.SenhaAtual, model.NovaSenha, ct);
        if (!ok)
        {
            ModelState.AddModelError(nameof(model.SenhaAtual), "Senha atual incorreta.");
            return View(model);
        }

        TempData["Sucesso"] = "Senha alterada com sucesso.";
        return RedirectToAction(nameof(Perfil));
    }

    private int UsuarioAtualId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task SignInAsync(Usuario usuario, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
        };
        if (usuario.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = isPersistent });
    }
}
