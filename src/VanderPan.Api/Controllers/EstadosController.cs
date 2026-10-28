using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VanderPan.Api.Data;
using VanderPan.Core.Models;

namespace VanderPan.Api.Controllers;

[ApiController]
[Route("api/estados")]
public class EstadosController : ControllerBase
{
    private readonly VanderPanDbContext _context;

    public EstadosController(VanderPanDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Estado>>> GetEstados()
    {
        return await _context.Estados.Where(e => e.Ativo).OrderBy(e => e.Nome).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Estado>> GetEstado(int id)
    {
        var estado = await _context.Estados.FindAsync(id);
        if (estado == null || !estado.Ativo) return NotFound();
        return estado;
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<Estado>> CreateEstado([FromBody] Estado estado)
    {
        if (await _context.Estados.AnyAsync(e => e.Uf.ToUpper() == estado.Uf.ToUpper()))
        {
            return BadRequest("UF já cadastrada.");
        }

        estado.Uf = estado.Uf.ToUpper();
        _context.Estados.Add(estado);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEstado), new { id = estado.Id }, estado);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] Estado estado)
    {
        if (id != estado.Id) return BadRequest();

        var existing = await _context.Estados.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Nome = estado.Nome;
        existing.Uf = estado.Uf.ToUpper();
        existing.Ativo = estado.Ativo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteEstado(int id)
    {
        var estado = await _context.Estados.FindAsync(id);
        if (estado == null) return NotFound();

        estado.Ativo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
