using GestaoEnderecos.Models;
using GestaoEnderecos.Services;
using GestaoEnderecos.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEnderecos.Controllers;

/// <summary>
/// CRUD de endereços do usuário autenticado. Controller fino: valida o input, delega ao
/// <see cref="EnderecoService"/> e devolve View/Redirect. O isolamento por usuário é garantido
/// na camada de dados (filtro global) — aqui não há lógica de "de quem é o registro".
/// </summary>
[Authorize]
public class EnderecosController : Controller
{
    private readonly EnderecoService _service;

    public EnderecosController(EnderecoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await _service.ListarAsync(ct));

    [HttpGet]
    public IActionResult Create() => View(new EnderecoFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EnderecoFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _service.CriarAsync(ParaEntidade(model), ct);
        TempData["Sucesso"] = "Endereço cadastrado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var endereco = await _service.ObterAsync(id, ct);
        return endereco is null ? NotFound() : View(ParaViewModel(endereco));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EnderecoFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ok = await _service.AtualizarAsync(id, ParaEntidade(model), ct);
        if (!ok)
        {
            return NotFound();
        }

        TempData["Sucesso"] = "Endereço atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await _service.ExcluirAsync(id, ct);
        if (!ok)
        {
            return NotFound();
        }

        TempData["Sucesso"] = "Endereço excluído.";
        return RedirectToAction(nameof(Index));
    }

    private static Endereco ParaEntidade(EnderecoFormViewModel m) => new()
    {
        Id = m.Id,
        Cep = m.Cep,
        Logradouro = m.Logradouro,
        Complemento = m.Complemento,
        Bairro = m.Bairro,
        Cidade = m.Cidade,
        Uf = m.Uf,
        Numero = m.Numero,
    };

    private static EnderecoFormViewModel ParaViewModel(Endereco e) => new()
    {
        Id = e.Id,
        Cep = e.Cep,
        Logradouro = e.Logradouro,
        Complemento = e.Complemento,
        Bairro = e.Bairro,
        Cidade = e.Cidade,
        Uf = e.Uf,
        Numero = e.Numero,
    };
}
