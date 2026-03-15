using FichaCosto.Service.Models.Enums;

namespace FichaCosto.Service.Models.DTOs;

/// <summary>
/// Resultado específico de validación de margen 30%
/// </summary>
public class ResultadoValidacionDto
{
    public bool EsValido { get; set; }
    public EstadoValidacion Estado { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string? DetalleTecnico { get; set; }

    // Datos del cálculo validado
    public decimal MargenUtilidadIngresado { get; set; }
    public decimal MargenMaximoPermitido { get; set; } = 30.0m;
    public decimal DiferenciaConMaximo { get; set; }

    // Referencias normativas
    public string ResolucionAplicable { get; set; } = "209/2024";
    public string? ArticuloAplicable { get; set; } = "Art. 3°";

    // Sugerencias
    public string? Sugerencia { get; set; }
}