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
[Route("api/contas-receber")]
public class ContasReceberController : ControllerBase
{
    private readonly VanderPanDbContext _context;

    public ContasReceberController(VanderPanDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContaReceber>>> GetContas(
        [FromQuery] StatusContaReceber? status,
        [FromQuery] int? idCliente,
        [FromQuery] DateTime? dataVencimentoInicio,
        [FromQuery] DateTime? dataVencimentoFim)
    {
        var query = _context.ContasReceber
            .Include(c => c.Cliente)
            .Include(c => c.Venda)
                .ThenInclude(v => v!.Condicao)
            .AsQueryable();

        var agora = DateTime.UtcNow.Date;

        if (status == StatusContaReceber.VENCIDO)
        {
            query = query.Where(c => c.Status == StatusContaReceber.VENCIDO ||
                (c.Status == StatusContaReceber.ABERTO && c.DataVencimento.Date < agora));
        }
        else if (status == StatusContaReceber.ABERTO)
        {
            query = query.Where(c => c.Status == StatusContaReceber.ABERTO &&
                c.DataVencimento.Date >= agora);
        }
        else if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (idCliente.HasValue)
        {
            query = query.Where(c => c.IdCliente == idCliente.Value);
        }

        if (dataVencimentoInicio.HasValue)
        {
            query = query.Where(c => c.DataVencimento >= dataVencimentoInicio.Value);
        }

        if (dataVencimentoFim.HasValue)
        {
            query = query.Where(c => c.DataVencimento <= dataVencimentoFim.Value);
        }

        var contas = await query.OrderBy(c => c.DataVencimento).ToListAsync();

        // Atualiza status para VENCIDO se data já expirou
        bool alterou = false;
        foreach (var c in contas)
        {
            if (c.Status == StatusContaReceber.ABERTO && c.DataVencimento.Date < agora)
            {
                c.Status = StatusContaReceber.VENCIDO;
                alterou = true;
            }
        }
        if (alterou) await _context.SaveChangesAsync();

        return Ok(contas);
    }

    [HttpGet("cliente/{id}")]
    public async Task<ActionResult<IEnumerable<ContaReceber>>> GetContasPorCliente(int id)
    {
        return await _context.ContasReceber
            .Include(c => c.Venda)
            .Where(c => c.IdCliente == id)
            .OrderBy(c => c.DataVencimento)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContaReceber>> GetConta(int id)
    {
        var conta = await _context.ContasReceber
            .Include(c => c.Cliente)
            .Include(c => c.Venda)
                .ThenInclude(v => v!.Condicao)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conta == null) return NotFound();
        return conta;
    }

    /// <summary>
    /// RF24 / RN09: Prévia do cálculo de multa, juros e desconto para uma determinada data de pagamento
    /// </summary>
    [HttpPost("{id}/calcular-liquidacao")]
    public async Task<ActionResult<CalculoPagamentoResultado>> CalcularLiquidacao(
        int id,
        [FromQuery] DateTime? dataPagamento)
    {
        var conta = await _context.ContasReceber
            .Include(c => c.Venda)
                .ThenInclude(v => v!.Condicao)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conta == null) return NotFound("Título não encontrado.");

        var condicao = conta.Venda?.Condicao;
        decimal multaPerc = condicao?.MultaPercentual ?? 0m;
        decimal jurosPerc = condicao?.JurosDiaPercentual ?? 0m;
        decimal descPerc = condicao?.DescontoDiaPercentual ?? 0m;

        var dtPag = dataPagamento ?? DateTime.UtcNow;

        var calculo = FinanceiroService.CalcularLiquidacao(
            conta.ValorOriginal,
            conta.DataVencimento,
            dtPag,
            multaPerc,
            jurosPerc,
            descPerc);

        return Ok(calculo);
    }

    /// <summary>
    /// RF23 / RF24 / RN09: Efetua a baixa (registro de pagamento) da parcela,
    /// aplicando cálculo automático de multa, juros e desconto.
    /// </summary>
    [HttpPatch("{id}/pagar")]
    public async Task<ActionResult<ContaReceber>> RegistrarPagamento(int id, [FromBody] BaixarTituloDTO dto)
    {
        var conta = await _context.ContasReceber
            .Include(c => c.Venda)
                .ThenInclude(v => v!.Condicao)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conta == null) return NotFound("Título não encontrado.");

        if (conta.Status == StatusContaReceber.PAGO)
        {
            return BadRequest("Este título já foi pago anteriormente.");
        }
        if (conta.Status == StatusContaReceber.CANCELADO)
        {
            return BadRequest("Não é possível baixar um título cancelado.");
        }

        var condicao = conta.Venda?.Condicao;
        decimal multaPerc = condicao?.MultaPercentual ?? 0m;
        decimal jurosPerc = condicao?.JurosDiaPercentual ?? 0m;
        decimal descPerc = condicao?.DescontoDiaPercentual ?? 0m;

        var dtPag = dto.DataPagamento.Date;

        var calculo = FinanceiroService.CalcularLiquidacao(
            conta.ValorOriginal,
            conta.DataVencimento,
            dtPag,
            multaPerc,
            jurosPerc,
            descPerc);

        conta.ValorMulta = calculo.Multa;
        conta.ValorJuros = calculo.Juros;
        conta.ValorDesconto = calculo.Desconto;
        conta.ValorPago = calculo.ValorFinal;
        conta.DataPagamento = dtPag;
        conta.Status = StatusContaReceber.PAGO;

        if (dto.FormaPagamento.HasValue)
        {
            conta.FormaPagamento = dto.FormaPagamento.Value;
        }

        await _context.SaveChangesAsync();

        return Ok(conta);
    }
}
