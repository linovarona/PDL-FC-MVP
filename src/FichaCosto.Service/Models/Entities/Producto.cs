using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FichaCosto.Service.Models.Enums;

namespace FichaCosto.Service.Models.Entities;

/// <summary>
/// Producto o servicio a costear
/// </summary>
[Table("Productos")]
public class Producto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

    public int Id { get; set; }
    [Required]
    public int ClienteId { get; set; }
    [ForeignKey("ClienteId")]
    public virtual Cliente Cliente { get; set; } = null!;
    [Required]
    [StringLength(50)]
    public string Codigo { get; set; } = string.Empty;
    [Required]
    [StringLength(200)]

    public string Nombre { get; set; } = string.Empty;
    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Required]
    public UnidadMedida UnidadMedida { get; set; } = UnidadMedida.Unidad;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // Relaciones
    public virtual ICollection<MateriaPrima> MateriasPrimas { get; set; } = new List<MateriaPrima>();
    public virtual ICollection<FichaCosto> FichasCosto { get; set; } = new List<FichaCosto>();
    public virtual ManoObraDirecta? ManoObra { get; set; }

    //Yo
    //ToDo:Porque ManoObraDirecta es una lista?
    //     Ver si es asi, porque no una coleccion como las otras
    public List<ManoObraDirecta> ManoObras { get; set; } = new();

}