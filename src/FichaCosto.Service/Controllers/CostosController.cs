using FichaCosto.Service.DTOs;
using FichaCosto.Service.Models.DTOs;
using FichaCosto.Service.Models.Entities;
using FichaCosto.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FichaCosto.Service.Controllers
{
    /// <summary>
    /// Controller para cálculo y validación de fichas de costo
    /// </summary>
    public class CostosController : ApiControllerBase
    {
        private readonly ICalculadoraCostoService _calculadora;
        private readonly IValidadorFichaService _validador;
        private readonly ILogger<CostosController> _logger;

        public CostosController(
            ICalculadoraCostoService calculadora,
            IValidadorFichaService validador,
            ILogger<CostosController> logger)
        {
            _calculadora = calculadora;
            _validador = validador;
            _logger = logger;
        }

        /// <summary>
        /// Calcula la ficha de costo completa según Res. 148/2023
        /// </summary>
        [HttpPost("calcular")]
        [SwaggerOperation(
            Summary = "Calcular ficha de costo",
            Description = "Calcula costos directos, precio de venta y valida margen del 30%"
        )]
        [SwaggerResponse(200, "Cálculo exitoso", typeof(ResultadoCalculoDto))]
        [SwaggerResponse(400, "Datos inválidos")]

        //public async Task<ActionResult<ResultadoCalculoDto>> Calcular([FromBody] FichaCostoDto request)
        //////public async Task<IActionResult> Calcular([FromBody] FichaCostoDto request)

        //{
        //    _logger.LogInformation("Iniciando cálculo de ficha para producto: {ProductoId}",
        //        request.ProductoId);

        //    try
        //    {
        //        //var resultado = await _calculadora.CalcularAsync(request); //Yo
        //        var resultado = await _calculadora.CalcularFichaCostoAsync(request.ProductoId, request.MargenUtilidad);



        //        _logger.LogInformation("Cálculo completado. Margen: {Margen}%, Válido: {Valido}",
        //            resultado.MargenUtilidad,
        //            resultado.EstadoValidacion);

        //        return Ok(resultado);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        _logger.LogWarning("Error de validación: {Message}", ex.Message);
        //        return ErrorResponse("Datos inválidos", ex.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error inesperado en cálculo");
        //        return StatusCode(500, new { error = "Error interno del servidor" });
        //    }
        //}

        //[HttpPost("calcular")]
        public async Task<IActionResult> Calcular([FromBody] FichaCostoDto request)
        {
            _logger.LogInformation("Iniciando cálculo de ficha para producto: {Producto}",
                request?.ProductoId.ToString() ?? "N/A");

            try
            {
                var resultado = await _calculadora.CalcularFichaCostoAsync(
                    request.ProductoId,
                    request.MargenUtilidad);

                _logger.LogInformation("Cálculo completado. Margen: {Margen}%, Válido: {Valido}",
                    resultado.MargenUtilidad,
                    resultado.EstadoValidacion);

                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Error de validación: {Message}", ex.Message);
                return ErrorResponse("Datos inválidos", ex.Message); // ✅ Ahora funciona
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en cálculo");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }







        /// <summary>
        /// Valida una ficha de costo sin realizar cálculos completos
        /// </summary>
        [HttpPost("validar")]
        [SwaggerOperation(
            Summary = "Validar ficha de costo",
            Description = "Valida que la ficha cumpla con Res. 209/2024 (margen máximo 30%)"
        )]
        [SwaggerResponse(200, "Validación exitosa", typeof(ResultadoValidacionDto))]
        [SwaggerResponse(400, "Datos inválidos")]
        public async Task<ActionResult<ResultadoValidacionDto>> Validar([FromBody] FichaCostoDto request)
        {
            _logger.LogInformation("Iniciando validación de ficha");

            try { 

                //  var resultado = await _validador.ValidarAsync(request);

                var resultado = await _validador.ValidarAsync(request);

                _logger.LogInformation("Validación completada. Válida: {Valida}, Errores: {Errores}",
                    resultado.EsValida,
                    resultado.Errores?.Count ?? 0);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en validación");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene información sobre las fórmulas de cálculo aplicadas
        /// </summary>
        //[HttpGet("formulas")]
        //[SwaggerOperation(Summary = "Obtener fórmulas de cálculo")]
        //public IActionResult ObtenerFormulas()
        //{
        //    return Ok(new
        //    {
        //        resolucion = "148/2023",
        //        formulas = new[]
        //        {
        //            new {
        //                concepto = "Costo Materias Primas",
        //                formula = "Σ(Cantidad × CostoUnitario)",
        //                ejemplo = "10 un × $15.50 = $155.00"
        //            },
        //            new {
        //                concepto = "Costo Mano de Obra",
        //                formula = "Horas × SalarioHora × (1 + CargasSociales/100)",
        //                ejemplo = "2.5h × $850 × 1.355 = $2,879.38"
        //            },
        //            new {
        //                concepto = "Precio de Venta",
        //                formula = "CostosDirectos × (1 + MargenUtilidad/100)",
        //                ejemplo = "$165 × 1.30 = $214.50"
        //            }
        //        },
        //        margenMaximo = new
        //        {
        //            resolucion = "209/2024",
        //            porcentaje = 30,
        //            descripcion = "Margen máximo de utilidad permitido"
        //        }
        //    });
        //}

        [HttpGet("formulas")]
        [SwaggerOperation(Summary = "Obtener fórmulas de cálculo")]
        [SwaggerResponse(200, "Fórmulas disponibles", typeof(FormulasCostoDto))]
        public IActionResult ObtenerFormulas()
        {
            var formulas = new FormulasCostoDto
            {
                Resolucion = "148/2023",
                Formulas = new List<FormulaDetalleDto>
        {
            new()
            {
                Concepto = "Costo Materias Primas",
                Formula = "Σ(Cantidad × CostoUnitario)",
                Ejemplo = "0.5 kg × $100/kg = $50"
            },
            new()
            {
                Concepto = "Costo Mano de Obra",
                Formula = "Horas × SalarioHora × (1 + CargasSociales/100)",
                Ejemplo = "2h × $50/h × 1.15 = $115"
            },
            new()
            {
                Concepto = "Precio de Venta",
                Formula = "CostosDirectos × (1 + MargenUtilidad/100)",
                Ejemplo = "$165 × 1.30 = $214.50"
            }
        },
                MargenMaximo = new MargenMaximoDto
                {
                    Resolucion = "209/2024",
                    Porcentaje = 30,
                    Descripcion = "Margen máximo de utilidad permitido"
                }
            };

            return Ok(formulas);
        }

    }
}