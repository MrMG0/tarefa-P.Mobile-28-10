using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VanderPan.Api.Data;
using VanderPan.Core.DTOs;
using VanderPan.Core.Enums;
using VanderPan.Core.Models;
using VanderPan.Core.Services;

namespace VanderPan.Api.Controllers;

[ApiController]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    private readonly VanderPanDbContext _context;

    public PedidosController(VanderPanDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidos(
        [FromQuery] StatusPedido? status,
        [FromQuery] JanelaEntrega? janela,
        [FromQuery] int? idVendedor,
        [FromQuery] int? idCliente)
    {
        var query = _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Vendedor)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
            .AsQueryable();

        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (janela.HasValue) query = query.Where(p => p.JanelaEntrega == janela.Value);
        if (idVendedor.HasValue) query = query.Where(p => p.IdVendedor == idVendedor.Value);
        if (idCliente.HasValue) query = query.Where(p => p.IdCliente == idCliente.Value);

        return await query.OrderByDescending(p => p.DataPedido).ToListAsync();
    }

    [HttpGet("vendedor/{id}")]
    public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidosPorVendedor(int id)
    {
        if (User.IsInRole("VENDEDOR"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var vendedorLogadoId))
            {
                return Unauthorized();
            }
            if (vendedorLogadoId != id) return Forbid();
        }

        return await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
            .Where(p => p.IdVendedor == id)
            .OrderByDescending(p => p.DataPedido)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Pedido>> GetPedido(int id)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Vendedor)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null) return NotFound();
        if (User.IsInRole("VENDEDOR") &&
            (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var vendedorLogadoId) ||
             pedido.IdVendedor != vendedorLogadoId))
        {
            return Forbid();
        }
        return pedido;
    }

    [HttpPost]
    [Authorize(Roles = "VENDEDOR")]
    public async Task<ActionResult<Pedido>> CreatePedido([FromBody] CriarPedidoDTO dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var vendedorId))
        {
            return Unauthorized();
        }

        var cliente = await _context.Clientes.FindAsync(dto.IdCliente);
        if (cliente == null || !cliente.Ativo || cliente.IdVendedor != vendedorId)
        {
            return BadRequest("Cliente não encontrado na carteira do vendedor.");
        }

        var vendedor = await _context.Vendedores.FindAsync(vendedorId);
        if (vendedor == null || !vendedor.Ativo) return BadRequest("Vendedor não encontrado.");

        if (dto.Itens == null || dto.Itens.Count == 0) return BadRequest("O pedido deve conter ao menos um item.");
        if (dto.Itens.Any(i => i.Quantidade <= 0 || decimal.Round(i.Quantidade, 3) != i.Quantidade))
        {
            return BadRequest("As quantidades devem ser positivas e ter no máximo três casas decimais.");
        }
        if (dto.Itens.Select(i => i.IdProduto).Distinct().Count() != dto.Itens.Count)
        {
            return BadRequest("Cada produto deve aparecer uma única vez no pedido.");
        }

        var pedido = new Pedido
        {
            IdVendedor = vendedorId,
            IdCliente = dto.IdCliente,
            DataPedido = DateTime.UtcNow,
            Status = StatusPedido.PENDENTE,
            JanelaEntrega = EntregaService.AlocarJanela(DateTime.UtcNow),
            Observacao = dto.Observacao,
            ValorTotal = 0m
        };

        decimal total = 0m;
        foreach (var itemDto in dto.Itens)
        {
            var produto = await _context.Produtos.FindAsync(itemDto.IdProduto);
            if (produto == null || !produto.Ativo || !produto.Disponivel)
            {
                return BadRequest($"Produto ID {itemDto.IdProduto} indisponível ou inexistente.");
            }

            var subtotal = Math.Round(produto.Preco * itemDto.Quantidade, 2);
            total += subtotal;

            pedido.Itens.Add(new ItemPedido
            {
                IdProduto = produto.Id,
                QuantidadePedida = itemDto.Quantidade,
                QuantidadeEntregue = itemDto.Quantidade, // Inicialmente igual
                PrecoUnitario = produto.Preco,
                Subtotal = subtotal
            });
        }

        pedido.ValorTotal = total;
        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPedido), new { id = pedido.Id }, pedido);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AtualizarStatus(int id, [FromBody] AtualizarStatusPedidoDTO dto)
    {
        var pedido = await _context.Pedidos.FindAsync(id);
        if (pedido == null) return NotFound();

        if (pedido.Status == dto.Status) return NoContent();

        var proximoStatus = pedido.Status switch
        {
            StatusPedido.PENDENTE => StatusPedido.CORTADO,
            StatusPedido.CORTADO => StatusPedido.CONFIRMADO,
            StatusPedido.CONFIRMADO => StatusPedido.EM_SEPARACAO,
            StatusPedido.EM_SEPARACAO => StatusPedido.ENVIADO,
            StatusPedido.ENVIADO => StatusPedido.ENTREGUE,
            _ => (StatusPedido?)null
        };
        var podeCancelar = pedido.Status is StatusPedido.PENDENTE or StatusPedido.CORTADO
            or StatusPedido.CONFIRMADO or StatusPedido.EM_SEPARACAO;

        if (dto.Status == StatusPedido.CANCELADO)
        {
            if (!podeCancelar)
            {
                return BadRequest("Só é permitido cancelar pedidos antes do envio.");
            }

            var venda = await _context.Vendas
                .Include(v => v.Itens)
                .FirstOrDefaultAsync(v => v.IdPedido == pedido.Id);
            if (venda != null)
            {
                if (await _context.ContasReceber.AnyAsync(c =>
                    c.IdVenda == venda.Id && c.Status == StatusContaReceber.PAGO))
                {
                    return BadRequest("Não é possível cancelar uma venda com títulos já pagos.");
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();
                foreach (var item in venda.Itens)
                {
                    var produto = await _context.Produtos.FindAsync(item.IdProduto);
                    if (produto != null) produto.Estoque += item.Quantidade;
                }

                var titulos = await _context.ContasReceber
                    .Where(c => c.IdVenda == venda.Id)
                    .ToListAsync();
                foreach (var titulo in titulos) titulo.Status = StatusContaReceber.CANCELADO;
                pedido.Status = StatusPedido.CANCELADO;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return NoContent();
            }
        }
        else if (dto.Status != proximoStatus)
        {
            return BadRequest($"Transição de status inválida: {pedido.Status} → {dto.Status}.");
        }

        pedido.Status = dto.Status;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// RF09 / RN06: Vendedor dispara os pedidos pendentes para a distribuidora.
    /// Aloca automaticamente na janela mais próxima (12h ou 19h).
    /// </summary>
    [HttpPost("enviar")]
    [Authorize(Roles = "VENDEDOR")]
    public async Task<ActionResult> DispararEnvio([FromBody] List<int> idsPedidos)
    {
        if (idsPedidos == null || idsPedidos.Count == 0 ||
            idsPedidos.Distinct().Count() != idsPedidos.Count)
        {
            return BadRequest("Informe uma lista não vazia de pedidos sem IDs duplicados.");
        }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var vendedorId))
        {
            return Unauthorized();
        }

        var agora = DateTime.UtcNow;
        var janela = EntregaService.AlocarJanela(agora);

        var pedidos = await _context.Pedidos
            .Where(p => idsPedidos.Contains(p.Id) &&
                        p.IdVendedor == vendedorId &&
                        p.Status == StatusPedido.PENDENTE)
            .ToListAsync();
        if (pedidos.Count != idsPedidos.Count)
        {
            return BadRequest("Um ou mais pedidos não pertencem ao vendedor ou não estão pendentes.");
        }

        foreach (var p in pedidos)
        {
            p.JanelaEntrega = janela;
        }

        await _context.SaveChangesAsync();
        return Ok(new
        {
            Mensagem = $"{pedidos.Count} pedido(s) disparado(s) com sucesso para a entrega das {EntregaService.FormatarJanela(janela)}.",
            JanelaAlocada = janela.ToString(),
            QuantidadePedidos = pedidos.Count
        });
    }

    /// <summary>
    /// RF20 / RN07: Calcula a sugestão de corte inteligente de pedidos para um produto.
    /// </summary>
    [HttpPost("corte/sugestao")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<List<ItemCorteSugestaoOutput>>> ObterSugestaoCorte(
        [FromQuery] int idProduto,
        [FromQuery] JanelaEntrega? janela)
    {
        var produto = await _context.Produtos.FindAsync(idProduto);
        if (produto == null) return NotFound("Produto não encontrado.");

        var query = _context.ItensPedido
            .Include(i => i.Pedido)
                .ThenInclude(p => p!.Cliente)
            .Where(i => i.IdProduto == idProduto && 
                       (i.Pedido!.Status == StatusPedido.PENDENTE || i.Pedido.Status == StatusPedido.CORTADO));

        if (janela.HasValue)
        {
            query = query.Where(i => i.Pedido!.JanelaEntrega == janela.Value);
        }

        var itens = await query.ToListAsync();
        if (!itens.Any())
        {
            return Ok(new List<ItemCorteSugestaoOutput>());
        }

        var clientesIds = itens.Select(i => i.Pedido!.IdCliente).Distinct().ToList();

        // Verifica débitos vencidos dos clientes envolvidos
        var agora = DateTime.UtcNow.Date;
        var debitosVencidos = await _context.ContasReceber
            .Where(c => clientesIds.Contains(c.IdCliente) && 
                       (c.Status == StatusContaReceber.VENCIDO || (c.Status == StatusContaReceber.ABERTO && c.DataVencimento.Date < agora)))
            .GroupBy(c => c.IdCliente)
            .Select(g => new { IdCliente = g.Key, TotalVencido = g.Sum(x => x.ValorOriginal) })
            .ToDictionaryAsync(x => x.IdCliente, x => x.TotalVencido);

        var inputs = itens.Select(i => new ItemCorteSugestaoInput
        {
            IdItemPedido = i.Id,
            IdPedido = i.IdPedido,
            IdCliente = i.Pedido!.IdCliente,
            NomeCliente = i.Pedido.Cliente?.RazaoSocial ?? "Cliente",
            QuantidadePedida = i.QuantidadePedida,
            PossuiDividaVencida = debitosVencidos.ContainsKey(i.Pedido.IdCliente) && debitosVencidos[i.Pedido.IdCliente] > 0,
            ValorDividaVencida = debitosVencidos.ContainsKey(i.Pedido.IdCliente) ? debitosVencidos[i.Pedido.IdCliente] : 0m
        }).ToList();

        var sugestao = CorteService.SugerirCorte(inputs, produto.Estoque);
        return Ok(sugestao);
    }

    /// <summary>
    /// RF20: Aplica o corte de pedidos após revisão e ajuste pelo operador da distribuidora.
    /// </summary>
    [HttpPost("corte")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> AplicarCorte([FromBody] AplicarCorteDTO dto)
    {
        if (dto.Itens == null || dto.Itens.Count == 0)
        {
            return BadRequest("Nenhum item informado para corte.");
        }
        if (dto.Itens.Select(i => i.IdItemPedido).Distinct().Count() != dto.Itens.Count)
        {
            return BadRequest("Cada item do pedido deve aparecer uma única vez no corte.");
        }

        var idsItens = dto.Itens.Select(i => i.IdItemPedido).ToList();
        var itens = await _context.ItensPedido
            .Include(i => i.Pedido)
            .Where(i => idsItens.Contains(i.Id))
            .ToListAsync();
        if (itens.Count != idsItens.Count ||
            itens.Any(i => i.Pedido == null ||
                i.Pedido.Status is not (StatusPedido.PENDENTE or StatusPedido.CORTADO)))
        {
            return BadRequest("Um ou mais itens não existem ou não podem mais ser cortados.");
        }

        var mapQuantidades = dto.Itens.ToDictionary(i => i.IdItemPedido, i => i.QuantidadeEntregue);
        if (itens.Any(i => mapQuantidades[i.Id] < 0 ||
                           mapQuantidades[i.Id] > i.QuantidadePedida ||
                           decimal.Round(mapQuantidades[i.Id], 3) != mapQuantidades[i.Id]))
        {
            return BadRequest("A quantidade entregue deve estar entre zero e a quantidade pedida, com até três casas decimais.");
        }

        var idsProdutos = itens.Select(i => i.IdProduto).Distinct().ToList();
        var itensReservados = await _context.ItensPedido
            .Include(i => i.Pedido)
            .Where(i => idsProdutos.Contains(i.IdProduto) &&
                        i.Pedido!.Status == StatusPedido.CORTADO &&
                        !idsItens.Contains(i.Id))
            .ToListAsync();
        foreach (var produtoId in idsProdutos)
        {
            var totalNovo = itens
                .Where(i => i.IdProduto == produtoId)
                .Sum(i => mapQuantidades[i.Id]);
            var totalReservado = itensReservados
                .Where(i => i.IdProduto == produtoId)
                .Sum(i => i.QuantidadeEntregue ?? i.QuantidadePedida);
            var produto = await _context.Produtos.FindAsync(produtoId);
            if (produto == null || totalNovo + totalReservado > produto.Estoque)
            {
                return BadRequest($"O corte excede o estoque disponível do produto {produtoId}.");
            }
        }

        var pedidosAfetados = new HashSet<Pedido>();

        foreach (var item in itens)
        {
            if (mapQuantidades.TryGetValue(item.Id, out var qtdEntregue))
            {
                item.QuantidadeEntregue = qtdEntregue;
                item.Subtotal = Math.Round(item.PrecoUnitario * qtdEntregue, 2);

                if (item.Pedido != null)
                {
                    item.Pedido.Status = StatusPedido.CORTADO;
                    pedidosAfetados.Add(item.Pedido);
                }
            }
        }

        // Recalcula valor total dos pedidos afetados
        foreach (var pedido in pedidosAfetados)
        {
            var itensDoPedido = await _context.ItensPedido
                .Where(i => i.IdPedido == pedido.Id)
                .ToListAsync();
            pedido.ValorTotal = itensDoPedido.Sum(i =>
                Math.Round((i.QuantidadeEntregue ?? i.QuantidadePedida) * i.PrecoUnitario, 2));
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mensagem = "Corte de pedidos aplicado com sucesso.",
            ItensAtualizados = itens.Count,
            PedidosAfetados = pedidosAfetados.Count
        });
    }
}
