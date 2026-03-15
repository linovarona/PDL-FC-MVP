using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FichaCosto.Service.Models.Enums;

namespace FichaCosto.Service.Models.Entities;

/// <summary>
/// Ficha de costo calculada para un producto
/// </summary>
[Table("FichasCosto")]
public class FichaCosto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProductoId { get; set; }

    [ForeignKey("ProductoId")]
    public virtual Producto Producto { get; set; } = null!;

    [Required]
    public DateTime FechaCalculo { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Suma de materias primas
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal CostoMateriasPrimas { get; set; }

    /// <summary>
    /// Costo de mano de obra directa
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal CostoManoObra { get; set; }

    /// <summary>
    /// Costos directos totales (MP + MO)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal CostosDirectosTotales { get; set; }

    /// <summary>
    /// Margen de utilidad aplicado (%)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MargenUtilidad { get; set; }

    /// <summary>
    /// Precio de venta calculado
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal PrecioVentaCalculado { get; set; }

    /// <summary>
    /// Estado de validación según Res. 209/2024
    /// </summary>
    [Required]
    //public EstadoValidacion EstadoValidacion { get; set; }
    public EstadoValidacion EstadoValidacion { get; set; } = EstadoValidacion.Valido;
    [StringLength(500)]
    public string? ObservacionesValidacion { get; set; }

    [StringLength(50)]
    public string? NumeroResolucionAplicada { get; set; } = "209/2024";

    /// <summary>
    /// Usuario o sistema que generó la ficha
    /// </summary>
    [StringLength(100)]
    public string? GeneradoPor { get; set; } = "Sistema";

    /// <summary>
    /// Versión del algoritmo de cálculo
    /// </summary>
    [StringLength(20)]
    public string? VersionCalculo { get; set; } = "1.0.0-MVP";
}