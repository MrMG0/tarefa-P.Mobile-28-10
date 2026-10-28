using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VanderPan.Api.Data;
using VanderPan.Core.Models;

namespace VanderPan.Api.Controllers;

[ApiController]
[Route("api/cidades")]
public class CidadesController : ControllerBase
{
    private readonly VanderPanDbContext _context;

    public CidadesController(VanderPanDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cidade>>> GetCidades([FromQuery] int? idEstado)
    {
        var query = _context.Cidades
            .Include(c => c.Estado)
            .Where(c => c.Ativo);

        if (idEstado.HasValue)
        {
            query = query.Where(c => c.IdEstado == idEstado.Value);
        }

        return await query.OrderBy(c => c.Nome).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Cidade>> GetCidade(int id)
    {
        var cidade = await _context.Cidades
            .Include(c => c.Estado)
            .FirstOrDefaultAsync(c => c.Id == id && c.Ativo);

        if (cidade == null) return NotFound();
        return cidade;
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<Cidade>> CreateCidade([FromBody] Cidade cidade)
    {
        var estadoExists = await _context.Estados.AnyAsync(e => e.Id == cidade.IdEstado && e.Ativo);
        if (!estadoExists) return BadRequest("Estado informado não existe ou está inativo.");

        _context.Cidades.Add(cidade);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCidade), new { id = cidade.Id }, cidade);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateCidade(int id, [FromBody] Cidade cidade)
    {
        if (id != cidade.Id) return BadRequest();

        var existing = await _context.Cidades.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Nome = cidade.Nome;
        existing.IdEstado = cidade.IdEstado;
        existing.Ativo = cidade.Ativo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteCidade(int id)
    {
        var cidade = await _context.Cidades.FindAsync(id);
        if (cidade == null) return NotFound();

        cidade.Ativo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
