using GestaoEnderecos.Data;
using GestaoEnderecos.Services;
using GestaoEnderecos.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEnderecos.Controllers;

/// <summary>
/// Área administrativa de usuários. Restrita a administradores (papel "Admin"). Inclui salvaguardas
/// contra lock-out: não é possível excluir a si mesmo nem remover/excluir o último administrador.
/// </summary>
[Authorize(Roles = "Admin")]
public class UsuariosController : Controller
{
    private readonly UsuarioService _usuarios;
    private readonly ICurrentUser _currentUser;

    public UsuariosController(UsuarioService usuarios, ICurrentUser currentUser)
    {
        _usuarios = usuarios;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await _usuarios.ListarAsync(ct));

    [HttpGet]
    public IActionResult Create() => View(new UsuarioCreateViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioCreateViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var r = await _usuarios.CriarAsync(model.Nome, model.Login, model.Senha, model.IsAdmin, ct);
        if (!r.Ok)
        {
            ModelState.AddModelError(nameof(model.Login), r.Erro!);
            return View(model);
        }

        TempData["Sucesso"] = "Usuário criado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var u = await _usuarios.ObterAsync(id, ct);
        return u is null
            ? NotFound()
            : View(new UsuarioEditViewModel { Id = u.Id, Nome = u.Nome, Login = u.Login, IsAdmin = u.IsAdmin });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UsuarioEditViewModel model, CancellationToken ct)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Salvaguarda: não rebaixar o último administrador.
        if (!model.IsAdmin)
        {
            var atual = await _usuarios.ObterAsync(id, ct);
            if (atual is { IsAdmin: true } && await _usuarios.ContarAdminsAsync(ct) <= 1)
            {
                ModelState.AddModelError(string.Empty, "Não é possível remover o último administrador.");
                return View(model);
            }
        }

        var r = await _usuarios.AtualizarAsync(id, model.Nome, model.Login, model.IsAdmin, ct);
        if (!r.Ok)
        {
            ModelState.AddModelError(nameof(model.Login), r.Erro!);
            return View(model);
        }

        TempData["Sucesso"] = "Usuário atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> RedefinirSenha(int id, CancellationToken ct)
    {
        var u = await _usuarios.ObterAsync(id, ct);
        return u is null
            ? NotFound()
            : View(new RedefinirSenhaViewModel { Id = u.Id, NomeUsuario = u.Nome });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedefinirSenha(int id, RedefinirSenhaViewModel model, CancellationToken ct)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ok = await _usuarios.RedefinirSenhaAsync(id, model.NovaSenha, ct);
        if (!ok)
        {
            return NotFound();
        }

        TempData["Sucesso"] = "Senha redefinida.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (id == _currentUser.Id)
        {
            TempData["Erro"] = "Você não pode excluir a si mesmo.";
            return RedirectToAction(nameof(Index));
        }

        var u = await _usuarios.ObterAsync(id, ct);
        if (u is null)
        {
            return NotFound();
        }

        if (u.IsAdmin && await _usuarios.ContarAdminsAsync(ct) <= 1)
        {
            TempData["Erro"] = "Não é possível excluir o último administrador.";
            return RedirectToAction(nameof(Index));
        }

        await _usuarios.ExcluirAsync(id, ct);
        TempData["Sucesso"] = "Usuário excluído.";
        return RedirectToAction(nameof(Index));
    }
}
