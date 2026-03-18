using FichaCosto.Service.Models.DTOs;
using FichaCosto.Service.Models.Entities;

namespace FichaCosto.Service.Services.Interfaces
{
    /// <summary>
    /// Servicio de cálculo de costos según Resolución 148/2023
    /// </summary>
    public interface ICalculadoraCostoService
    {
        /// <summary>
        /// Calcula una ficha de costo completa para un producto
        /// </summary>
        /// <param name="productoId">ID del producto</param>
        /// <param name="margenUtilidad">Porcentaje de margen deseado (0-30)</param>
        /// <returns>Resultado del cálculo con desglose</returns>
        Task<ResultadoCalculoDto> CalcularFichaCostoAsync(int productoId, decimal margenUtilidad);

        /// <summary>
        /// Calcula el costo de materias primas para un producto
        /// Fórmula: Σ(Cantidad × CostoUnitario)
        /// </summary>
        decimal CalcularCostoMateriasPrimas(IEnumerable<MateriaPrima> materiasPrimas);

        /// <summary>
        /// Calcula el costo de mano de obra directa
        /// Fórmula: Horas × SalarioHora × (1 + CargasSociales/100)
        /// </summary>
        decimal CalcularCostoManoObra(ManoObraDirecta manoObra);

        /// <summary>
        /// Calcula el precio de venta sugerido
        /// Fórmula: CostosDirectosTotales × (1 + MargenUtilidad/100)
        /// </summary>
        decimal CalcularPrecioVenta(decimal costosDirectosTotales, decimal margenUtilidad);

        /// <summary>
        /// Valida si el margen de utilidad cumple Res. 209/2024 (máximo 30%)
        /// </summary>
        bool EsMargenValido(decimal margenUtilidad);
    }
}