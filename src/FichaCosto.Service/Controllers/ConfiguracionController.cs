using FichaCosto.Service.DTOs;
using FichaCosto.Service.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FichaCosto.Service.Controllers
{
    /// <summary>
    /// Controller para configuración del sistema y catálogos
    /// </summary>
    public class ConfiguracionController : ApiControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ConfiguracionController> _logger;

        public ConfiguracionController(
            IConfiguration config,
            ILogger<ConfiguracionController> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene configuración completa del sistema
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Configuración del sistema")]
        [SwaggerResponse(200, "Configuración completa", typeof(ConfiguracionSistemaDto))]
        public IActionResult ObtenerConfiguracion()
        {
            var config = new ConfiguracionSistemaDto
            {
                Calculo = new CalculoConfigDto
                {
                    MargenUtilidadMaximo = 30.0,
                    DecimalesRedondeo = 2,
                    MetodoRepartoDefault = "HorasMaquina"
                },
                Resolucion = new ResolucionConfigDto
                {
                    Metodologia = "148/2023",
                    Margen = "209/2024",
                    FechaVigenciaMargen = "2024-07-01"
                },
                Api = new ApiConfigDto
                {
                    Version = "v1",
                    Entorno = _config["ASPNETCORE_ENVIRONMENT"] ?? "Production"
                }
            };

            _logger.LogInformation("Configuración solicitada. Entorno: {Entorno}", config.Api.Entorno);

            return Ok(config);
        }

        /// <summary>
        /// Obtiene información sobre resoluciones aplicadas
        /// </summary>
        [HttpGet("resoluciones")]
        [SwaggerOperation(Summary = "Información de resoluciones")]
        [SwaggerResponse(200, "Listado de resoluciones", typeof(List<ResolucionInfoDto>))]
        public IActionResult ObtenerResoluciones()
        {
            var resoluciones = new List<ResolucionInfoDto>
            {
                new()
                {
                    Numero = "148/2023",
                    Nombre = "Metodología de Costos",
                    Descripcion = "Establece la metodología para determinación de costos de producción",
                    FechaVigencia = "2023-01-01",
                    MargenMaximo = null, // No aplica
                    Activa = true
                },
                new()
                {
                    Numero = "209/2024",
                    Nombre = "Márgenes de Utilidad",
                    Descripcion = "Establece el margen máximo de utilidad permitido (30%)",
                    FechaVigencia = "2024-07-01",
                    MargenMaximo = 30.0,
                    Activa = true
                }
            };

            return Ok(resoluciones);
        }

        /// <summary>
        /// Obtiene unidades de medida disponibles
        /// </summary>
        [HttpGet("unidades-medida")]
        [SwaggerOperation(Summary = "Catálogo de unidades de medida")]
        [SwaggerResponse(200, "Listado de unidades", typeof(List<UnidadMedidaDto>))]
        public IActionResult ObtenerUnidadesMedida()
        {
            var unidades = Enum.GetValues(typeof(UnidadMedida))
                .Cast<UnidadMedida>()
                .Select(u => new UnidadMedidaDto
                {
                    Codigo = (int)u,
                    Nombre = u.ToString(),
                    Descripcion = u switch
                    {
                        UnidadMedida.Kilogramo => "Kilogramo (kg)",
                        UnidadMedida.Gramo => "Gramo (g)",
                        UnidadMedida.Litro => "Litro (L)",
                        UnidadMedida.Metro => "Metro (m)",
                        UnidadMedida.MetroCuadrado => "Metro cuadrado (m²)",
                        UnidadMedida.MetroCubico => "Metro cúbico (m³)",
                        UnidadMedida.Unidad => "Unidad (un)",
                        UnidadMedida.Hora => "Hora (h)",
                        UnidadMedida.Dia => "Día",
                        _ => u.ToString()
                    }
                })
                .ToList();

            return Ok(unidades);
        }

        /// <summary>
        /// Obtiene métodos de reparto de costos indirectos
        /// </summary>
        [HttpGet("metodos-reparto")]
        [SwaggerOperation(Summary = "Métodos de reparto de costos")]
        [SwaggerResponse(200, "Listado de métodos", typeof(List<MetodoRepartoDto>))]
        public IActionResult ObtenerMetodosReparto()
        {
            var metodos = new List<MetodoRepartoDto>
            {
                new()
                {
                    Codigo = "HorasMaquina",
                    Nombre = "Horas Máquina",
                    Descripcion = "Prorratea según horas máquina utilizadas por producto"
                },
                new()
                {
                    Codigo = "ValorProduccion",
                    Nombre = "Valor de Producción",
                    Descripcion = "Prorratea según valor de producción de cada producto"
                },
                new()
                {
                    Codigo = "UnidadesProducidas",
                    Nombre = "Unidades Producidas",
                    Descripcion = "Prorratea según cantidad de unidades producidas"
                }
            };

            return Ok(metodos);
        }
    }

    // ==================== DTOs Adicionales para otros endpoints ====================

    /// <summary>
    /// Información de una resolución normativa
    /// </summary>
    public class ResolucionInfoDto
    {
        public string Numero { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string FechaVigencia { get; set; } = string.Empty;
        public double? MargenMaximo { get; set; }
        public bool Activa { get; set; }
    }

    /// <summary>
    /// Unidad de medida disponible en el sistema
    /// </summary>
    public class UnidadMedidaDto
    {
        public int Codigo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Método de reparto de costos indirectos
    /// </summary>
    public class MetodoRepartoDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}