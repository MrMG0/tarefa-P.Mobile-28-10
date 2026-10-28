using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VanderPan.Core.Enums;

namespace VanderPan.Core.Models;

[Table("condicoes_pagamento")]
public class CondicaoPagamento
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("codigo")]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [Column("multa_percentual", TypeName = "decimal(5,2)")]
    public decimal MultaPercentual { get; set; } = 0;

    [Column("juros_dia_percentual", TypeName = "decimal(5,2)")]
    public decimal JurosDiaPercentual { get; set; } = 0;

    [Column("desconto_dia_percentual", TypeName = "decimal(5,2)")]
    public decimal DescontoDiaPercentual { get; set; } = 0;

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    public ICollection<ParcelaCondicao> Parcelas { get; set; } = new List<ParcelaCondicao>();
}

[Table("parcelas_condicao")]
public class ParcelaCondicao
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("id_condicao")]
    public int IdCondicao { get; set; }

    [ForeignKey("IdCondicao")]
    public CondicaoPagamento? Condicao { get; set; }

    [Column("numero_parcela")]
    public int NumeroParcela { get; set; }

    [Column("dias_prazo")]
    public int DiasPrazo { get; set; }

    [Column("percentual", TypeName = "decimal(5,2)")]
    public decimal Percentual { get; set; }

    [Column("forma_pagamento")]
    public FormaPagamento FormaPagamento { get; set; }
}
