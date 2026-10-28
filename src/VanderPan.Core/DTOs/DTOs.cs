using VanderPan.Core.Enums;
using VanderPan.Core.Models;

namespace VanderPan.Core.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class VendedorCadastroDTO
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string? Senha { get; set; }
    public bool Ativo { get; set; } = true;
}

public class LoginResponse
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Tipo { get; set; } = "VENDEDOR"; // "VENDEDOR" ou "ADMIN"
}

public class ItemPedidoInputDTO
{
    public int IdProduto { get; set; }
    public decimal Quantidade { get; set; }
}

public class CriarPedidoDTO
{
    public int IdVendedor { get; set; }
    public int IdCliente { get; set; }
    public string? Observacao { get; set; }
    public List<ItemPedidoInputDTO> Itens { get; set; } = new();
}

public class AtualizarStatusPedidoDTO
{
    public StatusPedido Status { get; set; }
}

public class ItemCorteInputDTO
{
    public int IdItemPedido { get; set; }
    public decimal QuantidadeEntregue { get; set; }
}

public class AplicarCorteDTO
{
    public List<ItemCorteInputDTO> Itens { get; set; } = new();
}

public class GerarVendaDTO
{
    public int IdPedido { get; set; }
    public int IdCondicao { get; set; }
    public string NumeroNota { get; set; } = string.Empty;
}

public class BaixarTituloDTO
{
    public DateTime DataPagamento { get; set; } = DateTime.UtcNow;
    public FormaPagamento? FormaPagamento { get; set; }
}

public class ItemUltimaCompraDTO
{
    public int IdProduto { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public DateTime DataCompra { get; set; }
}

public class ClienteDebitosDTO
{
    public int IdCliente { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public decimal TotalEmAberto { get; set; }
    public decimal TotalVencido { get; set; }
    public int QuantidadeTitulosAbertos { get; set; }
    public int QuantidadeTitulosVencidos { get; set; }
    public List<ContaReceber> Titulos { get; set; } = new();
}
