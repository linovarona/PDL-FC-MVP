namespace FichaCosto.Service.DTOs
{
    /// <summary>
    /// DTO para configuración completa del sistema
    /// </summary>
    public class ConfiguracionSistemaDto
    {
        public CalculoConfigDto Calculo { get; set; } = new();
        public ResolucionConfigDto Resolucion { get; set; } = new();
        public ApiConfigDto Api { get; set; } = new();
    }

    /// <summary>
    /// Configuración de cálculos de costos
    /// </summary>
    public class CalculoConfigDto
    {
        /// <summary>
        /// Margen máximo de utilidad permitido (%)
        /// </summary>
        public double MargenUtilidadMaximo { get; set; }

        /// <summary>
        /// Cantidad de decimales para redondeo
        /// </summary>
        public int DecimalesRedondeo { get; set; }

        /// <summary>
        /// Método de reparto de costos indirectos por defecto
        /// </summary>
        public string MetodoRepartoDefault { get; set; } = string.Empty;
    }

    /// <summary>
    /// Información de resoluciones normativas aplicadas
    /// </summary>
    public class ResolucionConfigDto
    {
        /// <summary>
        /// Número de resolución de metodología (148/2023)
        /// </summary>
        public string Metodologia { get; set; } = string.Empty;

        /// <summary>
        /// Número de resolución de márgenes (209/2024)
        /// </summary>
        public string Margen { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de vigencia del margen máximo
        /// </summary>
        public string FechaVigenciaMargen { get; set; } = string.Empty;
    }

    /// <summary>
    /// Información de la API y entorno
    /// </summary>
    public class ApiConfigDto
    {
        /// <summary>
        /// Versión de la API
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Entorno de ejecución (Development, Production, etc.)
        /// </summary>
        public string Entorno { get; set; } = string.Empty;
    }
}