using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VanderPan.Core.Enums;

namespace VanderPan.Core.Models;

[Table("pedidos")]
public class Pedido
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("id_vendedor")]
    public int IdVendedor { get; set; }

    [ForeignKey("IdVendedor")]
    public Vendedor? Vendedor { get; set; }

    [Required]
    [Column("id_cliente")]
    public int IdCliente { get; set; }

    [ForeignKey("IdCliente")]
    public Cliente? Cliente { get; set; }

    [Column("data_pedido")]
    public DateTime DataPedido { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public StatusPedido Status { get; set; } = StatusPedido.PENDENTE;

    [Column("janela_entrega")]
    public JanelaEntrega JanelaEntrega { get; set; } = JanelaEntrega.H12;

    [Column("observacao")]
    public string? Observacao { get; set; }

    [Column("valor_total", TypeName = "decimal(10,2)")]
    public decimal ValorTotal { get; set; }

    public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
}

[Table("itens_pedido")]
public class ItemPedido
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
    [Column("id_produto")]
    public int IdProduto { get; set; }

    [ForeignKey("IdProduto")]
    public Produto? Produto { get; set; }

    [Required]
    [Column("quantidade_pedida", TypeName = "decimal(10,3)")]
    public decimal QuantidadePedida { get; set; }

    [Column("quantidade_entregue", TypeName = "decimal(10,3)")]
    public decimal? QuantidadeEntregue { get; set; }

    [Required]
    [Column("preco_unitario", TypeName = "decimal(10,2)")]
    public decimal PrecoUnitario { get; set; }

    [Required]
    [Column("subtotal", TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }
}
