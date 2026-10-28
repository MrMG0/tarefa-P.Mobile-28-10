using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VanderPan.Core.Models;

[Table("vendedores")]
public class Vendedor
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("telefone")]
    public string? Telefone { get; set; }

    [Required]
    [MaxLength(14)]
    [Column("cpf")]
    public string Cpf { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("senha_hash")]
    [JsonIgnore]
    public string SenhaHash { get; set; } = string.Empty;

    [Column("data_cadastro")]
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    [JsonIgnore]
    public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();

    [JsonIgnore]
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    [JsonIgnore]
    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
}
