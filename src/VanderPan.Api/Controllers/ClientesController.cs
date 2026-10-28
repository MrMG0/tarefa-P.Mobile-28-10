using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VanderPan.Api.Data;
using VanderPan.Core.DTOs;
using VanderPan.Core.Enums;
using VanderPan.Core.Models;

namespace VanderPan.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly VanderPanDbContext _context;

    public ClientesController(VanderPanDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes(
        [FromQuery] int? idVendedor,
        [FromQuery] string? busca)
    {
        if (User.IsInRole("VENDEDOR"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var vendedorLogadoId))
            {
                return Unauthorized();
            }
            if (idVendedor.HasValue && idVendedor.Value != vendedorLogadoId)
            {
                return Forbid();
            }
            idVendedor = vendedorLogadoId;
        }

        var query = _context.Clientes
            .Include(c => c.Cidade)
                .ThenInclude(cid => cid!.Estado)
            .Include(c => c.Vendedor)
            .Where(c => c.Ativo);

        if (idVendedor.HasValue)
        {
            query = query.Where(c => c.IdVendedor == idVendedor.Value);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            query = query.Where(c => c.RazaoSocial.ToLower().Contains(termo) 
                                  || (c.NomeFantasia != null && c.NomeFantasia.ToLower().Contains(termo))
                                  || c.Cnpj.Contains(termo));
        }

        return await query.OrderBy(c => c.SequenciaVisita).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Cliente>> GetCliente(int id)
    {
        var cliente = await _context.Clientes
            .Include(c => c.Cidade)
                .ThenInclude(cid => cid!.Estado)
            .Include(c => c.Vendedor)
            .FirstOrDefaultAsync(c => c.Id == id && c.Ativo);

        if (cliente == null) return NotFound();
        if (!PodeAcessarCliente(cliente.IdVendedor)) return Forbid();
        return cliente;
    }

    [HttpGet("{id}/debitos")]
    public async Task<ActionResult<ClienteDebitosDTO>> GetDebitos(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null || !cliente.Ativo) return NotFound("Cliente não encontrado.");
        if (!PodeAcessarCliente(cliente.IdVendedor)) return Forbid();

        var titulos = await _context.ContasReceber
            .Where(c => c.IdCliente == id && (c.Status == StatusContaReceber.ABERTO || c.Status == StatusContaReceber.VENCIDO))
            .OrderBy(c => c.DataVencimento)
            .ToListAsync();

        var agora = DateTime.UtcNow.Date;

        // Atualiza status para VENCIDO se data já passou e estava ABERTO
        foreach (var t in titulos)
        {
            if (t.Status == StatusContaReceber.ABERTO && t.DataVencimento.Date < agora)
            {
                t.Status = StatusContaReceber.VENCIDO;
            }
        }
        await _context.SaveChangesAsync();

        var vencidos = titulos.Where(t => t.Status == StatusContaReceber.VENCIDO || t.DataVencimento.Date < agora).ToList();
        var abertos = titulos.Where(t => t.Status == StatusContaReceber.ABERTO && t.DataVencimento.Date >= agora).ToList();

        var resultado = new ClienteDebitosDTO
        {
            IdCliente = cliente.Id,
            RazaoSocial = cliente.RazaoSocial,
            TotalEmAberto = abertos.Sum(t => t.ValorOriginal),
            TotalVencido = vencidos.Sum(t => t.ValorOriginal),
            QuantidadeTitulosAbertos = abertos.Count,
            QuantidadeTitulosVencidos = vencidos.Count,
            Titulos = titulos
        };

        return Ok(resultado);
    }

    [HttpGet("{id}/historico")]
    public async Task<ActionResult<IEnumerable<ItemUltimaCompraDTO>>> GetHistorico(int id)
    {
        var clienteExists = await _context.Clientes.AnyAsync(c => c.Id == id && c.Ativo);
        if (!clienteExists) return NotFound("Cliente não encontrado.");
        if (User.IsInRole("VENDEDOR") &&
            !await _context.Clientes.AnyAsync(c => c.Id == id &&
                c.IdVendedor == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)))
        {
            return Forbid();
        }

        // Busca itens das vendas já realizadas para este cliente
        var itensVenda = await _context.ItensVenda
            .Include(iv => iv.Venda)
            .Include(iv => iv.Produto)
            .Where(iv => iv.Venda!.IdCliente == id)
            .OrderByDescending(iv => iv.Venda!.DataVenda)
            .ToListAsync();

        // Agrupa por produto para trazer a última compra de cada item
        var historico = itensVenda
            .GroupBy(iv => iv.IdProduto)
            .Select(g =>
            {
                var ultimo = g.First();
                return new ItemUltimaCompraDTO
                {
                    IdProduto = g.Key,
                    NomeProduto = ultimo.Produto?.Nome ?? "Produto",
                    Quantidade = ultimo.Quantidade,
                    PrecoUnitario = ultimo.PrecoUnitario,
                    Subtotal = ultimo.Subtotal,
                    DataCompra = ultimo.Venda?.DataVenda ?? DateTime.UtcNow
                };
            })
            .OrderByDescending(h => h.DataCompra)
            .ToList();

        return Ok(historico);
    }

    [HttpPost]
    public async Task<ActionResult<Cliente>> CreateCliente([FromBody] Cliente cliente)
    {
        if (User.IsInRole("VENDEDOR"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var vendedorLogadoId))
            {
                return Unauthorized();
            }
            cliente.IdVendedor = vendedorLogadoId;
        }

        var cnpjNormalizado = NormalizarDocumento(cliente.Cnpj);
        if (!CnpjValido(cnpjNormalizado) ||
            await _context.Clientes.AnyAsync(c =>
                c.Cnpj.Replace(".", "").Replace("/", "").Replace("-", "") == cnpjNormalizado))
        {
            return BadRequest("Informe um CNPJ válido que ainda não esteja cadastrado.");
        }
        cliente.Cnpj = cnpjNormalizado;

        var vendedorExists = await _context.Vendedores.AnyAsync(v => v.Id == cliente.IdVendedor && v.Ativo);
        if (!vendedorExists) return BadRequest("Vendedor informado não existe ou está inativo.");

        if (cliente.IdCidade.HasValue)
        {
            var cidadeExists = await _context.Cidades.AnyAsync(c => c.Id == cliente.IdCidade.Value && c.Ativo);
            if (!cidadeExists) return BadRequest("Cidade informada não existe ou está inativa.");
        }

        // Se a sequência de visita não for informada, calcula o próximo múltiplo de 10
        if (cliente.SequenciaVisita <= 0)
        {
            var maxSeq = await _context.Clientes
                .Where(c => c.IdVendedor == cliente.IdVendedor && c.Ativo)
                .Select(c => (int?)c.SequenciaVisita)
                .MaxAsync() ?? 0;

            cliente.SequenciaVisita = maxSeq + 10;
        }

        cliente.DataCadastro = DateTime.UtcNow;
        cliente.Ativo = true;

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateCliente(int id, [FromBody] Cliente cliente)
    {
        if (id != cliente.Id) return BadRequest();

        var existing = await _context.Clientes.FindAsync(id);
        if (existing == null) return NotFound();
        var cnpjNormalizado = NormalizarDocumento(cliente.Cnpj);
        if ((!CnpjValido(cnpjNormalizado) &&
             cnpjNormalizado != NormalizarDocumento(existing.Cnpj)) ||
            await _context.Clientes.AnyAsync(c => c.Id != id &&
                c.Cnpj.Replace(".", "").Replace("/", "").Replace("-", "") == cnpjNormalizado))
        {
            return BadRequest("Informe um CNPJ válido que ainda não esteja cadastrado.");
        }

        existing.RazaoSocial = cliente.RazaoSocial;
        existing.NomeFantasia = cliente.NomeFantasia;
        existing.Cnpj = cnpjNormalizado;
        existing.Email = cliente.Email;
        existing.Telefone = cliente.Telefone;
        existing.Cep = cliente.Cep;
        existing.Rua = cliente.Rua;
        existing.Numero = cliente.Numero;
        existing.Bairro = cliente.Bairro;
        existing.IdCidade = cliente.IdCidade;
        existing.IdVendedor = cliente.IdVendedor;
        existing.SequenciaVisita = cliente.SequenciaVisita;
        existing.Ativo = cliente.Ativo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteCliente(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();

        cliente.Ativo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool PodeAcessarCliente(int idVendedor) =>
        !User.IsInRole("VENDEDOR") ||
        (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var vendedorLogadoId) &&
         vendedorLogadoId == idVendedor);

    private static string NormalizarDocumento(string? documento) =>
        string.IsNullOrWhiteSpace(documento)
            ? string.Empty
            : new string(documento.Where(char.IsDigit).ToArray());

    private static bool CnpjValido(string cnpj)
    {
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1) return false;
        var digits = cnpj.Select(c => c - '0').ToArray();
        int[] firstWeights = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] secondWeights = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var firstDigit = CalcularDigitoCnpj(digits, firstWeights);
        var secondDigit = CalcularDigitoCnpj(digits, secondWeights);
        return digits[12] == firstDigit && digits[13] == secondDigit;
    }

    private static int CalcularDigitoCnpj(int[] digits, int[] weights)
    {
        var sum = weights.Select((weight, index) => weight * digits[index]).Sum();
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
