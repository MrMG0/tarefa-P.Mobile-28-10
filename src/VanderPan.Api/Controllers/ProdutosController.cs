using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VanderPan.Api.Data;
using VanderPan.Core.Models;

namespace VanderPan.Api.Controllers;

[ApiController]
[Route("api/produtos")]
public class ProdutosController : ControllerBase
{
    private readonly VanderPanDbContext _context;

    public ProdutosController(VanderPanDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Produto>>> GetProdutos(
        [FromQuery] int? idCategoria,
        [FromQuery] bool apenasDisponiveis = true)
    {
        var query = _context.Produtos
            .Include(p => p.Categoria)
            .Where(p => p.Ativo);

        if (apenasDisponiveis)
        {
            query = query.Where(p => p.Disponivel);
        }

        if (idCategoria.HasValue)
        {
            query = query.Where(p => p.IdCategoria == idCategoria.Value);
        }

        return await query.OrderBy(p => p.Nome).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Produto>> GetProduto(int id)
    {
        var prod = await _context.Produtos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id && p.Ativo);

        if (prod == null) return NotFound();
        return prod;
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<Produto>> CreateProduto([FromBody] Produto produto)
    {
        var catExists = await _context.Categorias.AnyAsync(c => c.Id == produto.IdCategoria && c.Ativo);
        if (!catExists) return BadRequest("Categoria informada não existe ou está inativa.");

        produto.DataCadastro = DateTime.UtcNow;
        produto.Ativo = true;

        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduto), new { id = produto.Id }, produto);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateProduto(int id, [FromBody] Produto produto)
    {
        if (id != produto.Id) return BadRequest();

        var existing = await _context.Produtos.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Nome = produto.Nome;
        existing.Descricao = produto.Descricao;
        existing.Preco = produto.Preco;
        existing.IdCategoria = produto.IdCategoria;
        existing.Disponivel = produto.Disponivel;
        existing.ImagemUrl = produto.ImagemUrl;
        existing.Estoque = produto.Estoque;
        existing.UnidadeMedida = produto.UnidadeMedida;
        existing.Perecivel = produto.Perecivel;
        existing.Ativo = produto.Ativo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteProduto(int id)
    {
        var prod = await _context.Produtos.FindAsync(id);
        if (prod == null) return NotFound();

        prod.Ativo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
