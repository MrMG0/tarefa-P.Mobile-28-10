using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using VanderPan.Api.Data;
using VanderPan.Api.Security;
using VanderPan.Core.DTOs;
using VanderPan.Core.Models;

namespace VanderPan.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly VanderPanDbContext _context;
    private readonly SessionTokenStore _sessions;

    public AuthController(VanderPanDbContext context, SessionTokenStore sessions)
    {
        _context = context;
        _sessions = sessions;
    }

    [HttpPost("vendedor/login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> LoginVendedor([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
        {
            return BadRequest(new LoginResponse
            {
                Sucesso = false,
                Mensagem = "E-mail e senha são obrigatórios."
            });
        }

        var vendedor = await _context.Vendedores
            .FirstOrDefaultAsync(v => v.Email.ToLower() == request.Email.Trim().ToLower() && v.Ativo);

        if (vendedor == null)
        {
            return Unauthorized(new LoginResponse
            {
                Sucesso = false,
                Mensagem = "E-mail ou senha inválidos."
            });
        }

        bool senhaValida;
        try
        {
            senhaValida = BCrypt.Net.BCrypt.Verify(request.Senha, vendedor.SenhaHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            senhaValida = false;
        }

        if (!senhaValida)
        {
            return Unauthorized(new LoginResponse
            {
                Sucesso = false,
                Mensagem = "E-mail ou senha inválidos."
            });
        }

        var token = CriarSessao(vendedor.Id, vendedor.Nome, vendedor.Email, "VENDEDOR");

        return Ok(new LoginResponse
        {
            Sucesso = true,
            Mensagem = "Autenticação realizada com sucesso.",
            Token = token,
            IdUsuario = vendedor.Id,
            Nome = vendedor.Nome,
            Email = vendedor.Email,
            Tipo = "VENDEDOR"
        });
    }

    [HttpPost("admin/login")]
    [AllowAnonymous]
    public ActionResult<LoginResponse> LoginAdmin([FromBody] LoginRequest request)
    {
        // Administrador padrão da distribuidora (admin@vanderpan.com.br / admin123)
        if (request.Email?.Trim().ToLower() == "admin@vanderpan.com.br" && request.Senha == "admin123")
        {
            var token = CriarSessao(999, "Administrador Geral", "admin@vanderpan.com.br", "ADMIN");
            return Ok(new LoginResponse
            {
                Sucesso = true,
                Mensagem = "Login de administrador realizado com sucesso.",
                Token = token,
                IdUsuario = 999,
                Nome = "Administrador Geral",
                Email = "admin@vanderpan.com.br",
                Tipo = "ADMIN"
            });
        }

        return Unauthorized(new LoginResponse
        {
            Sucesso = false,
            Mensagem = "Credenciais de administrador inválidas."
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _sessions.Revoke(authorization["Bearer ".Length..].Trim());
        }

        return NoContent();
    }

    private string CriarSessao(int userId, string nome, string email, string role) =>
        _sessions.Create(new ApiSession(
            userId,
            nome,
            email,
            role,
            DateTime.UtcNow.AddDays(30)));
}
