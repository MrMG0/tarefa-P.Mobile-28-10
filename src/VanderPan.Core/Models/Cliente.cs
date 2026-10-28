using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VanderPan.Core.Models;

[Table("clientes")]
public class Cliente
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("razao_social")]
    public string RazaoSocial { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("nome_fantasia")]
    public string? NomeFantasia { get; set; }

    [Required]
    [MaxLength(18)]
    [Column("cnpj")]
    public string Cnpj { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("email")]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Column("telefone")]
    public string? Telefone { get; set; }

    [MaxLength(10)]
    [Column("cep")]
    public string? Cep { get; set; }

    [MaxLength(200)]
    [Column("rua")]
    public string? Rua { get; set; }

    [MaxLength(20)]
    [Column("numero")]
    public string? Numero { get; set; }

    [MaxLength(100)]
    [Column("bairro")]
    public string? Bairro { get; set; }

    [Column("id_cidade")]
    public int? IdCidade { get; set; }

    [ForeignKey("IdCidade")]
    public Cidade? Cidade { get; set; }

    [Required]
    [Column("id_vendedor")]
    public int IdVendedor { get; set; }

    [ForeignKey("IdVendedor")]
    public Vendedor? Vendedor { get; set; }

    /// <summary>
    /// Sequência de visita na carteira do vendedor.
    /// Utiliza numeração espaçada (10, 20, 30...) para permitir inserções intermediárias (ex: 15 entre 10 e 20).
    /// </summary>
    [Column("sequencia_visita")]
    public int SequenciaVisita { get; set; }

    [Column("data_cadastro")]
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    [JsonIgnore]
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    [JsonIgnore]
    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();

    [JsonIgnore]
    public ICollection<ContaReceber> ContasReceber { get; set; } = new List<ContaReceber>();
}
