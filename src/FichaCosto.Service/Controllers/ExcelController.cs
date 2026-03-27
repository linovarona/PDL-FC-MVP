using FichaCosto.Service.DTOs;
using FichaCosto.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace FichaCosto.Service.Controllers
{
    /// <summary>
    /// Controller para importación y exportación de datos Excel
    /// </summary>
    public class ExcelController : ApiControllerBase
    {
        private readonly IExcelService _excelService;
        private readonly ICalculadoraCostoService _calculadora;
        private readonly ILogger<ExcelController> _logger;

        public ExcelController(
            IExcelService excelService,
            ICalculadoraCostoService calculadora,
            ILogger<ExcelController> logger)
        {
            _excelService = excelService;
            _calculadora = calculadora;
            _logger = logger;
        }

        /// <summary>
        /// Genera una plantilla Excel vacía para carga de datos
        /// </summary>
        [HttpGet("plantilla")]
        [SwaggerOperation(
            Summary = "Descargar plantilla Excel",
            Description = "Genera plantilla con estructura para materias primas y mano de obra"
        )]
        [SwaggerResponse(200, "Plantilla generada", typeof(FileStreamResult))]
        public async Task<IActionResult> DescargarPlantilla()
        {
            _logger.LogInformation("Generando plantilla Excel");

            try
            {
                var stream = await _excelService.GenerarPlantillaAsync();
                stream.Position = 0;

                return File(
                    stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"FichaCosto_Plantilla_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando plantilla");
                return StatusCode(500, new { error = "Error generando archivo" });
            }
        }

        /// <summary>
        /// Importa datos de producto desde archivo Excel (Materias Primas + Mano de Obra)
        /// </summary>
        [HttpPost("importar")]
        [SwaggerOperation(
            Summary = "Importar desde Excel",
            Description = "Carga materias primas y mano de obra desde archivo Excel"
        )]
        [SwaggerResponse(200, "Importación exitosa")]
        [SwaggerResponse(400, "Archivo inválido o formato incorrecto")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Importar([Required] IFormFile file)
        {
            _logger.LogInformation("Iniciando importación Excel: {Nombre}, {Size} bytes",
                file.FileName, file.Length);

            if (file == null || file.Length == 0)
            {
                return ErrorResponse("No se proporcionó archivo o está vacío");
            }

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return ErrorResponse("Solo se aceptan archivos .xlsx");
            }

            try
            {
                using var stream = file.OpenReadStream();

                // Usar ImportarTodo (materias primas + mano de obra)
                var resultado = await _excelService.ImportarDesdeExcelAsync(stream);

                _logger.LogInformation("Importación completada. MP: {Count}, MO: {TieneMO}",
                    resultado.MateriasPrimas?.Count() ?? 0,
                    resultado.ManoObra != null ? "Sí" : "No");

                return Ok(new ImportarExcelResponse
                {
                    Exitoso = true,
                    Archivo = file.FileName,
                    MateriasPrimasImportadas = resultado.MateriasPrimas?.Count() ?? 0,
                    ManoObraImportada = resultado.ManoObra != null,
                    HorasManoObra = resultado.ManoObra?.Horas ?? 0,
                    Mensaje = "Importación completada exitosamente"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Error de formato en importación: {Message}", ex.Message);
                return ErrorResponse("Formato de archivo inválido", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importando Excel");
                return StatusCode(500, new { error = "Error procesando archivo" });
            }
        }

        /// <summary>
        /// Exporta una ficha de costo calculada a Excel
        /// </summary>
        [HttpPost("exportar")]
        [SwaggerOperation(
            Summary = "Exportar ficha a Excel",
            Description = "Genera ficha de costo oficial en formato Excel"
        )]
        [SwaggerResponse(200, "Exportación exitosa", typeof(FileStreamResult))]
        [SwaggerResponse(400, "Datos inválidos")]
        public async Task<IActionResult> Exportar([FromBody] ExportarExcelRequest request)
        {
            _logger.LogInformation("Iniciando exportación de ficha. Producto: {ProductoId}",
                request?.ProductoId);

            if (request?.ProductoId <= 0)
            {
                return ErrorResponse("ID de producto inválido");
            }

            try
            {
                // Calcular primero para obtener ResultadoCalculoDto
                var resultadoCalculo = await _calculadora.CalcularFichaCostoAsync(
                    request.ProductoId,
                    request.MargenUtilidad);

                // Exportar usando el método correcto de la interfaz
                var stream = await _excelService.ExportarFichaCostoAsync(resultadoCalculo);
                stream.Position = 0;

                return File(
                    stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"FichaCosto_{request.ProductoId}_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando ficha");
                return StatusCode(500, new { error = "Error generando exportación" });
            }
        }
    }

    /// <summary>
    /// DTO para solicitud de exportación
    /// </summary>
    public class ExportarExcelRequest
    {
        [Required]
        public int ProductoId { get; set; }

        /// <summary>
        /// Margen de utilidad para el cálculo (%)
        /// </summary>
        [Range(0, 30, ErrorMessage = "El margen no puede exceder el 30%")]
        public decimal MargenUtilidad { get; set; } = 30.0m;
    }

    /// <summary>
    /// DTO para respuesta de importación
    /// </summary>
    public class ImportarExcelResponse
    {
        public bool Exitoso { get; set; }
        public string Archivo { get; set; } = string.Empty;
        public int MateriasPrimasImportadas { get; set; }
        public bool ManoObraImportada { get; set; }
        public decimal HorasManoObra { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}