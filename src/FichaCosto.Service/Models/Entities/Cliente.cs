using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FichaCosto.Service.Models.Entities;

/// <summary>
/// Cliente (PyME) que utiliza el sistema
/// </summary>
[Table("Clientes")]
public class Cliente
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string NombreEmpresa { get; set; } = string.Empty;

    [Required]
    [StringLength(11, MinimumLength = 11)]
    public string CUIT { get; set; } = string.Empty; // Formato: 30123456789

    [StringLength(500)]
    public string? Direccion { get; set; }

    [StringLength(100)]
    public string? ContactoNombre { get; set; }

    [StringLength(50)]
    public string? ContactoEmail { get; set; }

    [StringLength(20)]
    public string? ContactoTelefono { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;

    // Relaciones
    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}