using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using VanderPan.Core.Enums;

namespace VanderPan.Core.Models;

[Table("vendas")]
public class Venda
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("id_pedido")]
    public int IdPedido { get; set; }

    [ForeignKey("IdPedido")]
    public Pedido? Pedido { get; set; }

    [Required]
    [Column("id_cliente")]
    public int IdCliente { get; set; }

    [ForeignKey("IdCliente")]
    public Cliente? Cliente { get; set; }

    [Required]
    [Column("id_vendedor")]
    public int IdVendedor { get; set; }

    [ForeignKey("IdVendedor")]
    public Vendedor? Vendedor { get; set; }

    [Required]
    [Column("id_condicao")]
    public int IdCondicao { get; set; }

    [ForeignKey("IdCondicao")]
    public CondicaoPagamento? Condicao { get; set; }

    [Column("data_venda")]
    public DateTime DataVenda { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(20)]
    [Column("numero_nota")]
    public string NumeroNota { get; set; } = string.Empty;

    [Required]
    [Column("valor_total", TypeName = "decimal(10,2)")]
    public decimal ValorTotal { get; set; }

    public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();

    public ICollection<ContaReceber> ContasReceber { get; set; } = new List<ContaReceber>();
}

[Table("itens_venda")]
public class ItemVenda
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("id_venda")]
    public int IdVenda { get; set; }

    [ForeignKey("IdVenda")]
    [JsonIgnore]
    public Venda? Venda { get; set; }

    [Required]
    [Column("id_produto")]
    public int IdProduto { get; set; }

    [ForeignKey("IdProduto")]
    public Produto? Produto { get; set; }

    [Required]
    [Column("quantidade", TypeName = "decimal(10,3)")]
    public decimal Quantidade { get; set; }

    [Required]
    [Column("preco_unitario", TypeName = "decimal(10,2)")]
    public decimal PrecoUnitario { get; set; }

    [Required]
    [Column("subtotal", TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }
}

[Table("contas_receber")]
public class ContaReceber
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("id_venda")]
    public int IdVenda { get; set; }

    [ForeignKey("IdVenda")]
    [JsonIgnore]
    public Venda? Venda { get; set; }

    [Required]
    [Column("id_cliente")]
    public int IdCliente { get; set; }

    [ForeignKey("IdCliente")]
    public Cliente? Cliente { get; set; }

    [Column("numero_parcela")]
    public int NumeroParcela { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("numero_documento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Column("data_vencimento")]
    public DateTime DataVencimento { get; set; }

    [Required]
    [Column("valor_original", TypeName = "decimal(10,2)")]
    public decimal ValorOriginal { get; set; }

    [Column("valor_multa", TypeName = "decimal(10,2)")]
    public decimal ValorMulta { get; set; } = 0;

    [Column("valor_juros", TypeName = "decimal(10,2)")]
    public decimal ValorJuros { get; set; } = 0;

    [Column("valor_desconto", TypeName = "decimal(10,2)")]
    public decimal ValorDesconto { get; set; } = 0;

    [Column("valor_pago", TypeName = "decimal(10,2)")]
    public decimal ValorPago { get; set; } = 0;

    [Column("data_pagamento")]
    public DateTime? DataPagamento { get; set; }

    [Column("forma_pagamento")]
    public FormaPagamento FormaPagamento { get; set; }

    [Column("status")]
    public StatusContaReceber Status { get; set; } = StatusContaReceber.ABERTO;
}
