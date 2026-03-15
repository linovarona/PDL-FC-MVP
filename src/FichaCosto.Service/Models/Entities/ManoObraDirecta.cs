using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FichaCosto.Service.Models.Entities;

/// <summary>
/// Costo de mano de obra directa para un producto
/// </summary>
[Table("ManoObraDirecta")]
public class ManoObraDirecta
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProductoId { get; set; }

    [ForeignKey("ProductoId")]
    public virtual Producto Producto { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Horas { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal SalarioHora { get; set; }

    /// <summary>
    /// Porcentaje de cargas sociales (ej: 35.5 para 35.5%)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PorcentajeCargasSociales { get; set; } = 35.5m;

    /// <summary>
    /// Costo base = Horas × SalarioHora
    /// </summary>
    [NotMapped]
    public decimal CostoBase => Horas * SalarioHora;

    /// <summary>
    /// Costo total = CostoBase × (1 + CargasSociales/100)
    /// </summary>
    [NotMapped]
    public decimal CostoTotal => CostoBase * (1 + PorcentajeCargasSociales / 100);

    [StringLength(500)]
    public string? DescripcionTarea { get; set; }
}