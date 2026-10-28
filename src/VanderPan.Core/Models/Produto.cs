using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VanderPan.Core.Enums;

namespace VanderPan.Core.Models;

[Table("produtos")]
public class Produto
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("descricao")]
    public string? Descricao { get; set; }

    [Required]
    [Column("preco", TypeName = "decimal(10,2)")]
    public decimal Preco { get; set; }

    [Required]
    [Column("id_categoria")]
    public int IdCategoria { get; set; }

    [ForeignKey("IdCategoria")]
    public Categoria? Categoria { get; set; }

    [Column("disponivel")]
    public bool Disponivel { get; set; } = true;

    [MaxLength(500)]
    [Column("imagem_url")]
    public string? ImagemUrl { get; set; }

    [Column("estoque", TypeName = "decimal(10,3)")]
    public decimal Estoque { get; set; } = 0;

    [Column("unidade_medida")]
    public UnidadeMedida UnidadeMedida { get; set; } = UnidadeMedida.UNIDADE;

    [Column("perecivel")]
    public bool Perecivel { get; set; } = true;

    [Column("data_cadastro")]
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    [Column("ativo")]
    public bool Ativo { get; set; } = true;
}
