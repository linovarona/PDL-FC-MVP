using FichaCosto.Service.Models.Enums;

namespace FichaCosto.Service.Models.DTOs;

/// <summary>
/// Resultado del cálculo de ficha de costo
/// </summary>
public class ResultadoCalculoDto
{
    public int Id { get; set; } //Yo
    public int FichaCostoId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string ProductoCodigo { get; set; } = string.Empty;

    // Costos desglosados
    public decimal CostoMateriasPrimas { get; set; }
    public decimal CostoManoObra { get; set; }
    public decimal CostosDirectosTotales { get; set; }
    public decimal CostoTotal { get; set; } //Yo


    // Margen y precio
    public decimal MargenUtilidadAplicado { get; set; }
    public decimal PrecioVentaCalculado { get; set; }
    public decimal PrecioVentaSugerido { get; set; } //Yo
    

    // Validación
    public EstadoValidacion EstadoValidacion { get; set; }
    public string MensajeValidacion { get; set; } = string.Empty;

    // Metadata
    public DateTime? FechaCalculo { get; set; } 

    public string VersionSistema { get; set; } = "1.0.0-MVP";

    // Detalle para exportación
    public List<MateriaPrimaResultadoDto> DetalleMateriasPrimas { get; set; } = new();
    public ManoObraResultadoDto DetalleManoObra { get; set; } = new();
 
    //Yo:Nuevas

    public decimal MargenUtilidad { get; set; }
    public string NumeroResolucionAplicada{ get; set; } = string.Empty;
    public List<string> ObservacionesValidacion { get; set; } = new();
    public string Observaciones { get; set; } = string.Empty; //Yo
    public string CalculadoPor { get; set; } = string.Empty; //Yo
    


}

public class MateriaPrimaResultadoDto
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal CostoTotal { get; set; }
}

public class ManoObraResultadoDto
{
    public decimal Horas { get; set; }
    public decimal SalarioHora { get; set; }
    public decimal PorcentajeCargasSociales { get; set; }
    public decimal CostoBase { get; set; }
    public decimal CostoTotal { get; set; }
    public string? DescripcionTarea { get; set; }
}