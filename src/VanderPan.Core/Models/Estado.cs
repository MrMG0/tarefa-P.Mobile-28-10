using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VanderPan.Core.Models;

[Table("estados")]
public class Estado
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(2)]
    [Column("uf")]
    public string Uf { get; set; } = string.Empty;

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    [JsonIgnore]
    public ICollection<Cidade> Cidades { get; set; } = new List<Cidade>();
}
