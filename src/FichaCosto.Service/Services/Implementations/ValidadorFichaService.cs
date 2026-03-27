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

        /// <inheritdoc/>
        public async Task<ResultadoValidacionDto> ValidarAsync(FichaCostoDto ficha)
        {
            var mensajes = new List<string>();
            var errores = new List<string>();
            var esValido = true;

            _logger.LogInformation("Iniciando validación de ficha de entrada");

            // 1. Validar ProductoId
            if (ficha?.ProductoId <= 0)
            {
                errores.Add("El ID del producto es obligatorio y debe ser mayor a 0");
                esValido = false;
            }

            // 2. Validar margen de utilidad (directamente en la ficha, no en Configuracion)
            var margen = ficha?.MargenUtilidad ?? 0;

            if (!ValidarMargenGanancia(margen))
            {
                errores.Add($"El margen de utilidad ({margen}%) excede el límite máximo de 30% según Res. 209/2024");
                esValido = false;
            }
            else
            {
                var nivelAlerta = ObtenerNivelAlerta(margen);

                if (nivelAlerta == NivelAlertaMargen.Amarillo)
                {
                    mensajes.Add($"Advertencia: Margen de {margen}% está cercano al límite legal (30%)");
                }
                else if (nivelAlerta == NivelAlertaMargen.Verde)
                {
                    mensajes.Add($"Margen de {margen}% dentro de límites aceptables");
                }
            }

            // 3. Validar materias primas
            if (ficha?.MateriasPrimas == null || !ficha.MateriasPrimas.Any())
            {
                errores.Add("Debe especificar al menos una materia prima");
                esValido = false;
            }
            else
            {
                for (int i = 0; i < ficha.MateriasPrimas.Count; i++)
                {
                    var mp = ficha.MateriasPrimas[i];

                    if (string.IsNullOrWhiteSpace(mp.Nombre))
                    {
                        errores.Add($"Materia prima #{i + 1}: El nombre es obligatorio");
                        esValido = false;
                    }

                    // Usar 'Cantidad' en lugar de 'CantidadPorUnidad'
                    if (mp.Cantidad <= 0)
                    {
                        errores.Add($"Materia prima '{mp.Nombre ?? $"#{i + 1}"}': La cantidad debe ser mayor a 0");
                        esValido = false;
                    }

                    if (mp.CostoUnitario <= 0)
                    {
                        errores.Add($"Materia prima '{mp.Nombre ?? $"#{i + 1}"}': El costo unitario debe ser mayor a 0");
                        esValido = false;
                    }
                }
            }

            // 4. Validar mano de obra (usando la propiedad ManoObra, no ManoObraDirecta)
            if (ficha?.ManoObra == null)
            {
                errores.Add("La mano de obra es obligatoria");
                esValido = false;
            }
            else
            {
                if (ficha.ManoObra.SalarioHora <= 0)
                {
                    errores.Add("El salario por hora debe ser mayor a 0");
                    esValido = false;
                }

                if (ficha.ManoObra.Horas <= 0)
                {
                    errores.Add("Las horas de trabajo deben ser mayor a 0");
                    esValido = false;
                }

                // Validar porcentaje de cargas sociales (rango razonable)
                if (ficha.ManoObra.PorcentajeCargasSociales < 0 || ficha.ManoObra.PorcentajeCargasSociales > 100)
                {
                    errores.Add("El porcentaje de cargas sociales debe estar entre 0 y 100");
                    esValido = false;
                }
            }

            // Construir resultado
            var resultado = new ResultadoValidacionDto
            {
                EsValido = esValido,
                Estado = esValido ?
                    (mensajes.Any(m => m.Contains("Advertencia")) ? EstadoValidacion.ValidadaConObservaciones : EstadoValidacion.Validada)
                    : EstadoValidacion.Rechazada,
                Mensajes = mensajes,
                Errores = errores,
                FechaValidacion = DateTime.UtcNow,
                ResolucionAplicada = "209/2024"
            };

            _logger.LogInformation("Validación completada. Válida: {EsValido}, Errores: {Count}",
                esValido, errores.Count);

            return await Task.FromResult(resultado);
        }
    }
}