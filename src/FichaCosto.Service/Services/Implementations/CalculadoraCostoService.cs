using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Models.DTOs;
using FichaCosto.Service.Models.Entities;
using FichaCosto.Service.Models.Enums;
using FichaCosto.Service.Services.Interfaces;

namespace FichaCosto.Service.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de cálculo de costos
    /// Resolución 148/2023 - Metodología de costos
    /// Resolución 209/2024 - Margen máximo 30%
    /// </summary>
    public class CalculadoraCostoService : ICalculadoraCostoService
    {
        private readonly IProductoRepository _productoRepository;
        private readonly IValidadorFichaService _validador;
        private readonly ILogger<CalculadoraCostoService> _logger;

        // Constantes según normativa
        private const decimal MARGEN_MAXIMO_PERMITIDO = 30.0m;
        private const string NUMERO_RESOLUCION = "209/2024";

        public CalculadoraCostoService(
            IProductoRepository productoRepository,
            IValidadorFichaService validador,
            ILogger<CalculadoraCostoService> logger)
        {
            _productoRepository = productoRepository;
            _validador = validador;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<ResultadoCalculoDto> CalcularFichaCostoAsync(int productoId, decimal margenUtilidad)
        {
            _logger.LogInformation("Iniciando cálculo de ficha para ProductoId: {ProductoId}", productoId);

            // 1. Validar margen antes de calcular
            if (!EsMargenValido(margenUtilidad))
            {
                throw new ArgumentException(
                    $"El margen de utilidad {margenUtilidad}% excede el máximo permitido de {MARGEN_MAXIMO_PERMITIDO}% según Res. {NUMERO_RESOLUCION}");
            }

            // 2. Obtener producto con detalles
            var producto = await _productoRepository.GetByIdWithDetailsAsync(productoId);
            if (producto == null)
            {
                throw new KeyNotFoundException($"Producto con ID {productoId} no encontrado");
            }

            // 3. Calcular costos directos
            var costoMateriasPrimas = CalcularCostoMateriasPrimas(producto.MateriasPrimas.Where(mp => mp.Activo));
            var costoManoObra = producto.ManoObraDirecta != null
                ? CalcularCostoManoObra(producto.ManoObraDirecta)
                : 0m;

            var costosDirectosTotales = costoMateriasPrimas + costoManoObra;

            // 4. Calcular precio de venta
            var precioVenta = CalcularPrecioVenta(costosDirectosTotales, margenUtilidad);

            // 5. Preparar resultado
            var resultado = new ResultadoCalculoDto
            {
                ProductoId = productoId,
                ProductoNombre = producto.Nombre,
                CostoMateriasPrimas = costoMateriasPrimas,
                CostoManoObra = costoManoObra,
                CostosDirectosTotales = costosDirectosTotales,
                MargenUtilidad = margenUtilidad,
                PrecioVentaCalculado = precioVenta,
                FechaCalculo = DateTime.UtcNow,
                NumeroResolucionAplicada = NUMERO_RESOLUCION,
                EstadoValidacion = EstadoValidacion.Pendiente // Se validará después
            };

            // 6. Ejecutar validaciones de negocio
            var validacion = await _validador.ValidarCalculoAsync(resultado);
            resultado.EstadoValidacion = validacion.Estado;
            resultado.ObservacionesValidacion = validacion.Mensajes;

            _logger.LogInformation(
                "Cálculo completado para ProductoId: {ProductoId}. Costo: {Costo}, Precio: {Precio}, Estado: {Estado}",
                productoId, costosDirectosTotales, precioVenta, resultado.EstadoValidacion);

            return resultado;
        }

        /// <inheritdoc/>
        public decimal CalcularCostoMateriasPrimas(IEnumerable<MateriaPrima> materiasPrimas)
        {
            if (materiasPrimas == null || !materiasPrimas.Any())
            {
                _logger.LogWarning("No hay materias primas activas para calcular");
                return 0m;
            }

            var total = materiasPrimas.Sum(mp => mp.Cantidad * mp.CostoUnitario);

            _logger.LogDebug("Costo materias primas calculado: {Total}", total);

            return total;
        }

        /// <inheritdoc/>
        public decimal CalcularCostoManoObra(ManoObraDirecta manoObra)
        {
            if (manoObra == null)
            {
                _logger.LogWarning("No hay mano de obra configurada");
                return 0m;
            }

            // Fórmula: Horas × SalarioHora × (1 + CargasSociales/100)
            var factorCargas = 1 + (manoObra.PorcentajeCargasSociales / 100m);
            var costo = manoObra.Horas * manoObra.SalarioHora * factorCargas;

            _logger.LogDebug("Costo mano obra calculado: {Costo} (Horas: {Horas}, Salario: {Salario}, Cargas: {Cargas}%)",
                costo, manoObra.Horas, manoObra.SalarioHora, manoObra.PorcentajeCargasSociales);

            return costo;
        }

        /// <inheritdoc/>
        public decimal CalcularPrecioVenta(decimal costosDirectosTotales, decimal margenUtilidad)
        {
            if (costosDirectosTotales < 0)
            {
                throw new ArgumentException("Los costos directos no pueden ser negativos");
            }

            var factorMargen = 1 + (margenUtilidad / 100m);
            var precio = costosDirectosTotales * factorMargen;

            // Redondear a 2 decimales (pesos cubanos)
            return Math.Round(precio, 2);
        }

        /// <inheritdoc/>
        public bool EsMargenValido(decimal margenUtilidad)
        {
            return margenUtilidad >= 0 && margenUtilidad <= MARGEN_MAXIMO_PERMITIDO;
        }
    }
}