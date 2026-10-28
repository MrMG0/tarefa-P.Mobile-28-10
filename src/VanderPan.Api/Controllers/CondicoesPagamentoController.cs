using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VanderPan.Api.Data;
using VanderPan.Core.Models;

namespace VanderPan.Api.Controllers;

[ApiController]
[Route("api/condicoes-pagamento")]
public class CondicoesPagamentoController : ControllerBase
{
    private readonly VanderPanDbContext _context;

    public CondicoesPagamentoController(VanderPanDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CondicaoPagamento>>> GetCondicoes()
    {
        return await _context.CondicoesPagamento
            .Include(cp => cp.Parcelas.OrderBy(p => p.NumeroParcela))
            .Where(cp => cp.Ativo)
            .OrderBy(cp => cp.Codigo)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CondicaoPagamento>> GetCondicao(int id)
    {
        var cond = await _context.CondicoesPagamento
            .Include(cp => cp.Parcelas.OrderBy(p => p.NumeroParcela))
            .FirstOrDefaultAsync(cp => cp.Id == id && cp.Ativo);

        if (cond == null) return NotFound();
        return cond;
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<CondicaoPagamento>> CreateCondicao([FromBody] CondicaoPagamento condicao)
    {
        if (await _context.CondicoesPagamento.AnyAsync(c => c.Codigo == condicao.Codigo))
        {
            return BadRequest("Código de condição já cadastrado.");
        }

        condicao.Ativo = true;

        if (condicao.Parcelas != null)
        {
            foreach (var p in condicao.Parcelas)
            {
                p.Id = 0; // garante autoincrement
            }
        }

        _context.CondicoesPagamento.Add(condicao);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCondicao), new { id = condicao.Id }, condicao);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateCondicao(int id, [FromBody] CondicaoPagamento condicao)
    {
        if (id != condicao.Id) return BadRequest();

        var existing = await _context.CondicoesPagamento
            .Include(cp => cp.Parcelas)
            .FirstOrDefaultAsync(cp => cp.Id == id);

        if (existing == null) return NotFound();

        existing.Codigo = condicao.Codigo;
        existing.Descricao = condicao.Descricao;
        existing.MultaPercentual = condicao.MultaPercentual;
        existing.JurosDiaPercentual = condicao.JurosDiaPercentual;
        existing.DescontoDiaPercentual = condicao.DescontoDiaPercentual;
        existing.Ativo = condicao.Ativo;

        // Atualiza parcelas
        _context.ParcelasCondicao.RemoveRange(existing.Parcelas);
        if (condicao.Parcelas != null)
        {
            foreach (var p in condicao.Parcelas)
            {
                p.Id = 0;
                p.IdCondicao = existing.Id;
                existing.Parcelas.Add(p);
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteCondicao(int id)
    {
        var cond = await _context.CondicoesPagamento.FindAsync(id);
        if (cond == null) return NotFound();

        cond.Ativo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
