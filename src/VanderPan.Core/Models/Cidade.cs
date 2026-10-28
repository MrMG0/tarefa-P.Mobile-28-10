using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VanderPan.Core.Models;

[Table("cidades")]
public class Cidade
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [Column("id_estado")]
    public int IdEstado { get; set; }

    [ForeignKey("IdEstado")]
    public Estado? Estado { get; set; }

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    [JsonIgnore]
    public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
}
