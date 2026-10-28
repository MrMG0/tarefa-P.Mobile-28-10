using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VanderPan.Api.Data;
using VanderPan.Core.DTOs;
using VanderPan.Core.Enums;
using VanderPan.Core.Models;
using VanderPan.Core.Services;

namespace VanderPan.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/vendas")]
public class VendasController : ControllerBase
{
    private readonly VanderPanDbContext _context;

    public VendasController(VanderPanDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Venda>>> GetVendas()
    {
        return await _context.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Vendedor)
            .Include(v => v.Condicao)
            .Include(v => v.ContasReceber)
            .OrderByDescending(v => v.DataVenda)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Venda>> GetVenda(int id)
    {
        var venda = await _context.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Vendedor)
            .Include(v => v.Condicao)
                .ThenInclude(c => c!.Parcelas)
            .Include(v => v.Itens)
                .ThenInclude(i => i.Produto)
            .Include(v => v.ContasReceber)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venda == null) return NotFound();
        return venda;
    }

    /// <summary>
    /// RF21 / RF22 / RN08 / RN09: Converte pedido confirmado/cortado em Venda,
    /// baixa estoque dos produtos entregues e gera automaticamente as parcelas de contas a receber.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Venda>> CriarVenda([FromBody] GerarVendaDTO dto)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == dto.IdPedido);

        if (pedido == null) return NotFound("Pedido não encontrado.");

        if (pedido.Status is not (StatusPedido.CORTADO or StatusPedido.CONFIRMADO))
        {
            return BadRequest("Só é possível gerar venda após o corte e a confirmação do pedido.");
        }

        if (await _context.Vendas.AnyAsync(v => v.IdPedido == pedido.Id))
        {
            return Conflict("Este pedido já foi convertido em venda.");
        }

        var condicao = await _context.CondicoesPagamento
            .Include(cp => cp.Parcelas)
            .FirstOrDefaultAsync(cp => cp.Id == dto.IdCondicao && cp.Ativo);

        if (condicao == null) return BadRequest("Condição de pagamento não encontrada.");

        if (string.IsNullOrWhiteSpace(dto.NumeroNota))
        {
            return BadRequest("Número da nota fiscal é obrigatório.");
        }
        if (condicao.Parcelas.Count == 0 ||
            condicao.Parcelas.Any(p => p.NumeroParcela <= 0 || p.DiasPrazo < 0 ||
                p.Percentual <= 0 || p.Percentual > 100) ||
            condicao.Parcelas.Select(p => p.NumeroParcela).Distinct().Count() != condicao.Parcelas.Count ||
            Math.Abs(condicao.Parcelas.Sum(p => p.Percentual) - 100m) > 0.01m)
        {
            return BadRequest("A condição de pagamento deve ter parcelas válidas cuja soma dos percentuais seja 100%.");
        }
        if (pedido.Itens.Count == 0 ||
            pedido.Itens.Any(i => (i.QuantidadeEntregue ?? i.QuantidadePedida) < 0 ||
                (i.QuantidadeEntregue ?? i.QuantidadePedida) > i.QuantidadePedida))
        {
            return BadRequest("O pedido contém quantidades de entrega inválidas.");
        }

        var totaisPorProduto = pedido.Itens
            .Select(i => new
            {
                i.IdProduto,
                Quantidade = i.QuantidadeEntregue ?? i.QuantidadePedida
            })
            .Where(i => i.Quantidade > 0)
            .GroupBy(i => i.IdProduto)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantidade));

        foreach (var item in pedido.Itens)
        {
            var quantidade = item.QuantidadeEntregue ?? item.QuantidadePedida;
            if (quantidade > 0 &&
                !await _context.Produtos.AnyAsync(p => p.Id == item.IdProduto && p.Ativo && p.Disponivel))
            {
                return BadRequest($"Produto {item.IdProduto} não está mais disponível.");
            }
        }

        var totalVenda = pedido.Itens.Sum(i =>
            Math.Round(i.PrecoUnitario * (i.QuantidadeEntregue ?? i.QuantidadePedida), 2));
        if (totalVenda <= 0) return BadRequest("O valor total da venda deve ser maior que zero.");

        var venda = new Venda
        {
            IdPedido = pedido.Id,
            IdCliente = pedido.IdCliente,
            IdVendedor = pedido.IdVendedor,
            IdCondicao = condicao.Id,
            DataVenda = DateTime.UtcNow,
            NumeroNota = dto.NumeroNota.Trim(),
            ValorTotal = totalVenda,
            Itens = pedido.Itens
                .Where(i => (i.QuantidadeEntregue ?? i.QuantidadePedida) > 0)
                .Select(i => new ItemVenda
                {
                    IdProduto = i.IdProduto,
                    Quantidade = i.QuantidadeEntregue ?? i.QuantidadePedida,
                    PrecoUnitario = i.PrecoUnitario,
                    Subtotal = Math.Round(i.PrecoUnitario * (i.QuantidadeEntregue ?? i.QuantidadePedida), 2)
                })
                .ToList()
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        foreach (var (produtoId, quantidade) in totaisPorProduto)
        {
            var atualizados = await _context.Produtos
                .Where(p => p.Id == produtoId && p.Ativo && p.Disponivel && p.Estoque >= quantidade)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(p => p.Estoque, p => p.Estoque - quantidade));
            if (atualizados != 1)
            {
                await transaction.RollbackAsync();
                return Conflict($"Estoque insuficiente para o produto {produtoId}.");
            }
        }

        _context.Vendas.Add(venda);
        await _context.SaveChangesAsync();

        // Gera automaticamente as parcelas de contas a receber
        var parcelas = FinanceiroService.GerarParcelas(venda, condicao);
        foreach (var parcela in parcelas)
        {
            parcela.IdVenda = venda.Id;
            _context.ContasReceber.Add(parcela);
        }

        // Atualiza status do pedido para CONFIRMADO
        pedido.Status = StatusPedido.CONFIRMADO;
        pedido.ValorTotal = totalVenda;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return CreatedAtAction(nameof(GetVenda), new { id = venda.Id }, venda);
    }
}
