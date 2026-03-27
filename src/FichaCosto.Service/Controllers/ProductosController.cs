using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.DTOs;
using FichaCosto.Service.Mappings;
using FichaCosto.Service.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FichaCosto.Service.Controllers
{
    /// <summary>
    /// Controller para gestión de productos y sus componentes
    /// </summary>
    public class ProductosController : ApiControllerBase
    {
        private readonly IProductoRepository _productoRepo;
        private readonly ILogger<ProductosController> _logger;

        public ProductosController(
            IProductoRepository productoRepo,
            ILogger<ProductosController> logger)
        {
            _productoRepo = productoRepo;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene productos de un cliente específico
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        [SwaggerOperation(Summary = "Listar productos por cliente")]
        public async Task<ActionResult<List<ProductoDto>>> ObtenerPorCliente(int clienteId)
        {
            var entities = await _productoRepo.GetByClienteIdAsync(clienteId);

            // ✅ Materializar explícitamente con ToList()
            var dtos = entities.Select(e => e.ToDto()).ToList();

            _logger.LogInformation("Obtenidos {Count} productos para ClienteId: {ClienteId}",
                dtos.Count, clienteId);

            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene un producto con todos sus detalles (MP y MO)
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Obtener producto completo")]
        [SwaggerResponse(200, "Producto con materias primas y mano de obra")]
        public async Task<ActionResult<ProductoCompletoDto>> ObtenerPorId(int id)
        {
            var entity = await _productoRepo.GetByIdWithDetailsAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            // ✅ Materializar explícitamente con ToList()
            var materiasPrimasDtos = entity.MateriasPrimas?
                .Where(mp => mp.Activo)
                .Select(mp => mp.ToDto())
                .ToList() ?? new List<MateriaPrimaDto>();

            var resultado = new ProductoCompletoDto
            {
                Producto = entity.ToDto(),
                MateriasPrimas = materiasPrimasDtos,
                ManoObra = entity.ManoObraDirecta
            };

            return Ok(resultado);
        }

        /// <summary>
        /// Crea un nuevo producto
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Crear producto")]
        [SwaggerResponse(201, "Producto creado")]
        public async Task<ActionResult<ProductoDto>> Crear([FromBody] ProductoDto dto)
        {
            // Verificar código único por cliente
            if (await _productoRepo.ExistsByCodigoAsync(dto.Codigo, null))
            {
                return BadRequest(new { error = "Ya existe un producto con ese código" });

                ////return ErrorResponse("Ya existe un producto con ese código");
            }

            var entity = new Models.Entities.Producto
            {
                ClienteId = dto.ClienteId,
                Codigo = dto.Codigo,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                UnidadMedida = dto.UnidadMedida,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var id = await _productoRepo.CreateAsync(entity);
            dto.Id = id;

            _logger.LogInformation("Producto creado: {Id} - {Codigo}", id, dto.Codigo);

            return CreatedAtAction(nameof(ObtenerPorId), new { id }, dto);
        }

        /// <summary>
        /// Actualiza un producto
        /// </summary>
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Actualizar producto")]
        public async Task<ActionResult<ProductoDto>> Actualizar(int id, [FromBody] ProductoDto dto)
        {
            

            if (id != dto.Id) return BadRequest(new { error = "ID no coincide" });

            var existente = await _productoRepo.GetByIdAsync(id);
            if (existente == null) return NotFound();

            // Verificar código único si cambió
            if (existente.Codigo != dto.Codigo &&
                await _productoRepo.ExistsByCodigoAsync(dto.Codigo, id))
            {
                return BadRequest(new { error = "Ya existe un producto con ese código" });

                //return ErrorResponse("Ya existe otro producto con ese código");
            }

            existente.Codigo = dto.Codigo;
            existente.Nombre = dto.Nombre;
            existente.Descripcion = dto.Descripcion;
            existente.UnidadMedida = dto.UnidadMedida;
            existente.Activo = dto.Activo;
            existente.FechaModificacion = DateTime.UtcNow;

            var exito = await _productoRepo.UpdateAsync(existente);
            if (!exito) return StatusCode(500, new { error = "Error actualizando producto" });

            return Ok(existente.ToDto());
        }

        /// <summary>
        /// Elimina un producto (hard delete)
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Eliminar producto")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var exito = await _productoRepo.DeleteAsync(id);
            if (!exito) return NotFound();

            _logger.LogInformation("Producto eliminado: {Id}", id);

            return NoContent();
        }
    }

    /// <summary>
    /// DTO para producto con todas sus relaciones
    /// </summary>
    public class ProductoCompletoDto
    {
        public ProductoDto Producto { get; set; } = null!;
        public List<MateriaPrimaDto> MateriasPrimas { get; set; } = new List<MateriaPrimaDto>();
        public Models.Entities.ManoObraDirecta? ManoObra { get; set; }
    }
}