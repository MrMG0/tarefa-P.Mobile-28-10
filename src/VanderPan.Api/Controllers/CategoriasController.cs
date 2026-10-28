using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VanderPan.Api.Data;
using VanderPan.Core.Models;

namespace VanderPan.Api.Controllers;

[ApiController]
[Route("api/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly VanderPanDbContext _context;

    public CategoriasController(VanderPanDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Categoria>>> GetCategorias()
    {
        return await _context.Categorias
            .Where(c => c.Ativo)
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Categoria>> GetCategoria(int id)
    {
        var cat = await _context.Categorias.FindAsync(id);
        if (cat == null || !cat.Ativo) return NotFound();
        return cat;
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<Categoria>> CreateCategoria([FromBody] Categoria categoria)
    {
        categoria.Ativo = true;
        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategoria), new { id = categoria.Id }, categoria);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateCategoria(int id, [FromBody] Categoria categoria)
    {
        if (id != categoria.Id) return BadRequest();

        var existing = await _context.Categorias.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Nome = categoria.Nome;
        existing.Descricao = categoria.Descricao;
        existing.Ativo = categoria.Ativo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteCategoria(int id)
    {
        var cat = await _context.Categorias.FindAsync(id);
        if (cat == null) return NotFound();

        cat.Ativo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
