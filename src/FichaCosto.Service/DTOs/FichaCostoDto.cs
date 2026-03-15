using System.ComponentModel.DataAnnotations;
using FichaCosto.Service.Models.Enums;

namespace FichaCosto.Service.Models.DTOs;

/// <summary>
/// DTO para solicitar cálculo de ficha de costo
/// </summary>
public class FichaCostoDto
{
    [Required(ErrorMessage = "El ID del producto es obligatorio")]
    [Range(1, int.MaxValue, ErrorMessage = "ID de producto inválido")]
    public int ProductoId { get; set; }

    [Required(ErrorMessage = "Debe especificar al menos una materia prima")]
    [MinLength(1, ErrorMessage = "Mínimo una materia prima requerida")]
    public List<MateriaPrimaInputDto> MateriasPrimas { get; set; } = new();

    [Required(ErrorMessage = "La mano de obra es obligatoria")]
    public ManoObraInputDto ManoObra { get; set; } = new();

    /// <summary>
    /// Margen de utilidad deseado (%). Máximo 30% según Res. 209/2024
    /// </summary>
    [Required]
    [Range(0, 30, ErrorMessage = "El margen no puede exceder el 30% según Res. 209/2024")]
    public decimal MargenUtilidad { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }
}

public class MateriaPrimaInputDto
{
    [Required]
    [StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(50)]
    public string? CodigoInterno { get; set; }

    [Required]
    [Range(0.0001, 999999999, ErrorMessage = "Cantidad debe ser mayor a 0")]
    public decimal Cantidad { get; set; }

    [Required]
    [Range(0.0001, 999999999, ErrorMessage = "Costo unitario debe ser mayor a 0")]
    public decimal CostoUnitario { get; set; }

    public int Orden { get; set; }
}

public class ManoObraInputDto
{
    [Required]
    [Range(0.01, 999999, ErrorMessage = "Horas debe ser mayor a 0")]
    public decimal Horas { get; set; }

    [Required]
    [Range(0.01, 999999, ErrorMessage = "Salario por hora debe ser mayor a 0")]
    public decimal SalarioHora { get; set; }

    [Range(0, 100)]
    public decimal PorcentajeCargasSociales { get; set; } = 35.5m;

    [StringLength(500)]
    public string? DescripcionTarea { get; set; }
}