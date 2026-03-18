using FichaCosto.Service.Models.DTOs;
using FichaCosto.Service.Models.Enums;
using FichaCosto.Service.Services.Interfaces;

namespace FichaCosto.Service.Services.Implementations
{
    /// <summary>
    /// Implementación del validador de fichas de costo
    /// Resolución 209/2024 - Márgenes de utilidad
    /// </summary>
    public class ValidadorFichaService : IValidadorFichaService
    {
        private readonly ILogger<ValidadorFichaService> _logger;

        // Umbrales según Res. 209/2024
        private const decimal MARGEN_MAXIMO_LEGAL = 30.0m;
        private const decimal MARGEN_ALERTA = 25.0m;
        private const string RESOLUCION_APLICABLE = "209/2024";

        public ValidadorFichaService(ILogger<ValidadorFichaService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<ResultadoValidacionDto> ValidarCalculoAsync(ResultadoCalculoDto resultado)
        {
            var mensajes = new List<string>();
            var errores = new List<string>();
            var estado = EstadoValidacion.Validada;

            // 1. Validar margen de utilidad
            if (!ValidarMargenGanancia(resultado.MargenUtilidad))
            {
                errores.Add($"El margen de utilidad ({resultado.MargenUtilidad}%) excede el límite máximo de {MARGEN_MAXIMO_LEGAL}% según Res. {RESOLUCION_APLICABLE}");
                estado = EstadoValidacion.Rechazada;

                _logger.LogWarning("Validación fallida: Margen {Margen}% excede límite legal", resultado.MargenUtilidad);
            }
            else
            {
                var nivelAlerta = ObtenerNivelAlerta(resultado.MargenUtilidad);

                switch (nivelAlerta)
                {
                    case NivelAlertaMargen.Amarillo:
                        mensajes.Add($"Advertencia: Margen de utilidad cercano al límite máximo ({resultado.MargenUtilidad}%). Límite legal: {MARGEN_MAXIMO_LEGAL}%");
                        estado = EstadoValidacion.ValidadaConObservaciones;
                        _logger.LogInformation("Margen cercano al límite: {Margen}%", resultado.MargenUtilidad);
                        break;

                    case NivelAlertaMargen.Rojo:
                        // No debería llegar aquí si ValidarMargenGanancia funciona correctamente
                        errores.Add("Error interno: Margen excede límite pero pasó validación inicial");
                        estado = EstadoValidacion.Error;
                        break;

                    default: // Verde
                        mensajes.Add($"Margen de utilidad dentro de límites aceptables ({resultado.MargenUtilidad}%)");
                        break;
                }
            }

            // 2. Validar que costos sean positivos
            if (resultado.CostosDirectosTotales < 0)
            {
                errores.Add("El costo directo total no puede ser negativo");
                estado = EstadoValidacion.Rechazada;
            }

            if (resultado.CostoMateriasPrimas < 0)
            {
                errores.Add("El costo de materias primas no puede ser negativo");
                estado = EstadoValidacion.Rechazada;
            }

            if (resultado.CostoManoObra < 0)
            {
                errores.Add("El costo de mano de obra no puede ser negativo");
                estado = EstadoValidacion.Rechazada;
            }

            // 3. Validar coherencia matemática
            var costoCalculado = resultado.CostoMateriasPrimas + resultado.CostoManoObra;
            if (Math.Abs(costoCalculado - resultado.CostosDirectosTotales) > 0.01m)
            {
                errores.Add($"Incoherencia en cálculo: Suma de costos ({costoCalculado}) no coincide con total ({resultado.CostosDirectosTotales})");
                estado = EstadoValidacion.Error;
            }

            // 4. Validar precio de venta
            var precioEsperado = resultado.CostosDirectosTotales * (1 + resultado.MargenUtilidad / 100m);
            if (Math.Abs(precioEsperado - resultado.PrecioVentaCalculado) > 0.01m)
            {
                errores.Add($"Incoherencia en precio: Cálculo esperado {precioEsperado}, obtenido {resultado.PrecioVentaCalculado}");
                estado = EstadoValidacion.Error;
            }

            // Construir resultado
            var resultadoValidacion = new ResultadoValidacionDto
            {
                EsValida = estado == EstadoValidacion.Validada || estado == EstadoValidacion.ValidadaConObservaciones,
                Estado = estado,
                Mensajes = mensajes,
                Errores = errores,
                FechaValidacion = DateTime.UtcNow,
                ResolucionAplicada = RESOLUCION_APLICABLE
            };

            _logger.LogInformation(
                "Validación completada. Estado: {Estado}, Errores: {CountErrores}, Mensajes: {CountMensajes}",
                estado, errores.Count, mensajes.Count);

            return await Task.FromResult(resultadoValidacion);
        }

        /// <inheritdoc/>
        public bool ValidarMargenGanancia(decimal margenUtilidad)
        {
            return margenUtilidad >= 0 && margenUtilidad <= MARGEN_MAXIMO_LEGAL;
        }

        /// <inheritdoc/>
        public NivelAlertaMargen ObtenerNivelAlerta(decimal margenUtilidad)
        {
            if (margenUtilidad > MARGEN_MAXIMO_LEGAL)
                return NivelAlertaMargen.Rojo;

            if (margenUtilidad >= MARGEN_ALERTA)
                return NivelAlertaMargen.Amarillo;

            return NivelAlertaMargen.Verde;
        }
    }
}