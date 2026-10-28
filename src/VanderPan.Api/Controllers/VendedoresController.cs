using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VanderPan.Core.DTOs;
using VanderPan.Api.Data;
using VanderPan.Core.Models;

namespace VanderPan.Api.Controllers;

[ApiController]
[Route("api/vendedores")]
public class VendedoresController : ControllerBase
{
    private readonly VanderPanDbContext _context;

    public VendedoresController(VanderPanDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<IEnumerable<Vendedor>>> GetVendedores()
    {
        return await _context.Vendedores
            .Where(v => v.Ativo)
            .OrderBy(v => v.Nome)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<Vendedor>> GetVendedor(int id)
    {
        var vendedor = await _context.Vendedores.FindAsync(id);
        if (vendedor == null || !vendedor.Ativo) return NotFound();
        return vendedor;
    }

    [HttpGet("{id}/carteira")]
    public async Task<ActionResult<IEnumerable<Cliente>>> GetCarteiraVendedor(int id)
    {
        if (User.IsInRole("VENDEDOR"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var vendedorLogadoId))
            {
                return Unauthorized();
            }
            if (vendedorLogadoId != id) return Forbid();
        }

        var vendedorExists = await _context.Vendedores.AnyAsync(v => v.Id == id && v.Ativo);
        if (!vendedorExists) return NotFound("Vendedor não encontrado.");

        var clientes = await _context.Clientes
            .Include(c => c.Cidade)
                .ThenInclude(cid => cid!.Estado)
            .Where(c => c.IdVendedor == id && c.Ativo)
            .OrderBy(c => c.SequenciaVisita)
            .ToListAsync();

        return Ok(clientes);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<Vendedor>> CreateVendedor([FromBody] VendedorCadastroDTO dto)
    {
        var cpfNormalizado = NormalizarDocumento(dto.Cpf);
        if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest("Nome e e-mail são obrigatórios.");
        }
        if (string.IsNullOrWhiteSpace(dto.Senha))
        {
            return BadRequest("Senha do vendedor é obrigatória.");
        }
        if (!CpfValido(cpfNormalizado))
        {
            return BadRequest("Informe um CPF válido.");
        }
        if (await _context.Vendedores.AnyAsync(v => v.Email.ToLower() == dto.Email.Trim().ToLower()))
        {
            return BadRequest("E-mail já cadastrado para outro vendedor.");
        }

        if (await _context.Vendedores.AnyAsync(v =>
            v.Cpf.Replace(".", "").Replace("-", "") == cpfNormalizado))
        {
            return BadRequest("CPF já cadastrado.");
        }

        var vendedor = new Vendedor
        {
            Nome = dto.Nome.Trim(),
            Email = dto.Email.Trim(),
            Telefone = dto.Telefone,
            Cpf = cpfNormalizado,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
            Ativo = true
        };

        vendedor.DataCadastro = DateTime.UtcNow;
        vendedor.Ativo = true;

        _context.Vendedores.Add(vendedor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetVendedor), new { id = vendedor.Id }, vendedor);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateVendedor(int id, [FromBody] VendedorCadastroDTO dto)
    {
        var cpfNormalizado = NormalizarDocumento(dto.Cpf);
        if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest("Nome e e-mail são obrigatórios.");
        }
        var existing = await _context.Vendedores.FindAsync(id);
        if (existing == null) return NotFound();
        if (!CpfValido(cpfNormalizado) &&
            cpfNormalizado != NormalizarDocumento(existing.Cpf))
        {
            return BadRequest("Informe um CPF válido.");
        }

        if (await _context.Vendedores.AnyAsync(v => v.Id != id &&
            (v.Email.ToLower() == dto.Email.Trim().ToLower() ||
             v.Cpf.Replace(".", "").Replace("-", "") == cpfNormalizado)))
        {
            return BadRequest("O e-mail ou CPF informado já está cadastrado.");
        }

        existing.Nome = dto.Nome.Trim();
        existing.Email = dto.Email.Trim();
        existing.Telefone = dto.Telefone;
        existing.Cpf = cpfNormalizado;
        existing.Ativo = dto.Ativo;

        if (!string.IsNullOrWhiteSpace(dto.Senha))
        {
            existing.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteVendedor(int id)
    {
        var vendedor = await _context.Vendedores.FindAsync(id);
        if (vendedor == null) return NotFound();

        vendedor.Ativo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static string NormalizarDocumento(string? documento) =>
        string.IsNullOrWhiteSpace(documento)
            ? string.Empty
            : new string(documento.Where(char.IsDigit).ToArray());

    private static bool CpfValido(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1) return false;
        var digits = cpf.Select(c => c - '0').ToArray();
        var firstSum = digits.Take(9).Select((digit, index) => digit * (10 - index)).Sum();
        var first = firstSum % 11 < 2 ? 0 : 11 - firstSum % 11;
        var secondSum = digits.Take(10).Select((digit, index) => digit * (11 - index)).Sum();
        var second = secondSum % 11 < 2 ? 0 : 11 - secondSum % 11;
        return digits[9] == first && digits[10] == second;
    }
}
