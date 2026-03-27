using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

// Alias para resolver conflicto de nombres
using FichaCostoEntity = FichaCosto.Service.Models.Entities.FichaCosto;

namespace FichaCosto.Service.Controllers
{
    /// <summary>
    /// Controller para gestión de fichas de costo calculadas
    /// </summary>
    public class FichasController : ApiControllerBase
    {
        private readonly IFichaRepository _fichaRepo;
        private readonly ILogger<FichasController> _logger;

        public FichasController(
            IFichaRepository fichaRepo,
            ILogger<FichasController> logger)
        {
            _fichaRepo = fichaRepo;
            _logger = logger;
        }

        /// <summary>
        /// Crea/guarda una ficha de costo calculada
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Guardar ficha calculada")]
        [SwaggerResponse(201, "Ficha guardada")]
        public async Task<ActionResult<ResultadoCalculoDto>> Crear([FromBody] ResultadoCalculoDto dto)
        {
            var entity = new FichaCostoEntity
            {
                ProductoId = dto.ProductoId,
                FechaCalculo = DateTime.UtcNow,
                CostoMateriasPrimas = dto.CostoMateriasPrimas,
                CostoManoObra = dto.CostoManoObra,
                CostosDirectosTotales = dto.CostosDirectosTotales,
                MargenUtilidad = dto.MargenUtilidad,
                PrecioVentaCalculado = dto.PrecioVentaCalculado,
                EstadoValidacion = dto.EstadoValidacion,
                ObservacionesValidacion = dto.ObservacionesValidacion,
                CostoTotal = dto.CostoTotal,
                PrecioVentaSugerido = dto.PrecioVentaSugerido,
                Observaciones = dto.Observaciones,
                CalculadoPor = dto.CalculadoPor ?? "Sistema"
            };

            var id = await _fichaRepo.CreateAsync(entity);
            dto.Id = id;

            _logger.LogInformation("Ficha guardada: {Id} - Producto: {ProductoId}",
                id, dto.ProductoId);

            return CreatedAtAction(nameof(ObtenerPorId), new { id }, dto);
        }

        /// <summary>
        /// Obtiene una ficha por su ID
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Obtener ficha por ID")]
        public async Task<ActionResult<ResultadoCalculoDto>> ObtenerPorId(int id)
        {
            var entity = await _fichaRepo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            var dto = MapToDto(entity);
            return Ok(dto);
        }

        /// <summary>
        /// Obtiene historial de fichas de un producto
        /// </summary>
        [HttpGet("producto/{productoId}/historial")]
        [SwaggerOperation(Summary = "Historial de fichas por producto")]
        public async Task<ActionResult<List<ResultadoCalculoDto>>> ObtenerHistorial(
            int productoId,
            [FromQuery] int cantidad = 10)
        {
            var entities = await _fichaRepo.GetHistorialByProductoIdAsync(productoId, cantidad);

            // ✅ Materializar explícitamente con ToList() antes de retornar
            var dtos = entities.Select(entity => MapToDto(entity)).ToList();

            _logger.LogInformation("Historial obtenido: {Count} fichas para ProductoId: {ProductoId}",
                dtos.Count, productoId);

            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene la última ficha calculada de un producto
        /// </summary>
        [HttpGet("producto/{productoId}/ultima")]
        [SwaggerOperation(Summary = "Última ficha de producto")]
        public async Task<ActionResult<ResultadoCalculoDto>> ObtenerUltima(int productoId)
        {
            var entity = await _fichaRepo.GetUltimaFichaByProductoIdAsync(productoId);
            if (entity == null) return NotFound();

            var dto = MapToDto(entity);
            return Ok(dto);
        }

        #region Métodos Privados

        private static ResultadoCalculoDto MapToDto(FichaCostoEntity entity)
        {
            return new ResultadoCalculoDto
            {
                Id = entity.Id,
                ProductoId = entity.ProductoId,
                FechaCalculo = entity.FechaCalculo,
                CostoMateriasPrimas = entity.CostoMateriasPrimas,
                CostoManoObra = entity.CostoManoObra,
                CostosDirectosTotales = entity.CostosDirectosTotales,
                MargenUtilidad = entity.MargenUtilidad,
                PrecioVentaCalculado = entity.PrecioVentaCalculado,
                EstadoValidacion = entity.EstadoValidacion,
                ObservacionesValidacion = entity.ObservacionesValidacion,
                CostoTotal = entity.CostoTotal,
                PrecioVentaSugerido = entity.PrecioVentaSugerido,
                CalculadoPor = entity.CalculadoPor
            };
        }

        #endregion
    }
}