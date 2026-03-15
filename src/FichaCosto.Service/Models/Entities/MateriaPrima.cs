using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FichaCosto.Service.Models.Entities;

/// <summary>
/// Materia prima o insumo directo para un producto
/// </summary>
[Table("MateriasPrimas")]
public class MateriaPrima
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProductoId { get; set; }

    [ForeignKey("ProductoId")]
    public virtual Producto Producto { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(50)]
    public string? CodigoInterno { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Cantidad { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal CostoUnitario { get; set; }

    /// <summary>
    /// Costo total = Cantidad × CostoUnitario
    /// </summary>
    [NotMapped]
    public decimal CostoTotal => Cantidad * CostoUnitario;

    [StringLength(500)]
    public string? Observaciones { get; set; }

    public int Orden { get; set; } = 0; // Para ordenar en la ficha

    public bool Activo { get; set; } = true;
}