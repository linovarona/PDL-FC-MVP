using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.DTOs;
using FichaCosto.Service.Mappings;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FichaCosto.Service.Controllers
{
    /// <summary>
    /// Controller para gestión de clientes (PyMEs)
    /// </summary>
    public class ClientesController : ApiControllerBase
    {
        private readonly IClienteRepository _clienteRepo;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(
            IClienteRepository clienteRepo,
            ILogger<ClientesController> logger)
        {
            _clienteRepo = clienteRepo;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los clientes activos
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Listar clientes")]
        [SwaggerResponse(200, "Lista de clientes", typeof(IEnumerable<ClienteDto>))]
        public async Task<ActionResult<IEnumerable<ClienteDto>>> ObtenerTodos()
        {
            var entities = await _clienteRepo.GetAllAsync();
            var dtos = entities.ToDtoList();
            return Ok(dtos);
        }
        
        /// <summary>
        /// Obtiene un cliente por su ID
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Obtener cliente por ID")]
        [SwaggerResponse(200, "Cliente encontrado", typeof(ClienteDto))]
        [SwaggerResponse(404, "Cliente no encontrado")]
        public async Task<ActionResult<ClienteDto>> ObtenerPorId(int id)
        {
            var entity = await _clienteRepo.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound(new { error = $"Cliente {id} no encontrado" });
            }
            return Ok(entity.ToDto());
        }

        /// <summary>
        /// Crea un nuevo cliente
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Crear cliente")]
        [SwaggerResponse(201, "Cliente creado", typeof(ClienteDto))]
        [SwaggerResponse(400, "Datos inválidos")]
        public async Task<IActionResult> Crear([FromBody] ClienteDto dto)
        {
            // Verificar CUIT único
            if (await _clienteRepo.ExistsByCuitAsync(dto.CUIT))
            {
                return ErrorResponse("Ya existe un cliente con ese CUIT");
            }

            // Convertir DTO → Entity
            var entity = new Models.Entities.Cliente
            {
                NombreEmpresa = dto.NombreEmpresa,
                CUIT = dto.CUIT,
                Direccion = dto.Direccion,
                ContactoNombre = dto.ContactoNombre,
                ContactoEmail = dto.ContactoEmail,
                ContactoTelefono = dto.ContactoTelefono,
                Activo = true,
                FechaAlta = DateTime.UtcNow
            };

            var id = await _clienteRepo.CreateAsync(entity);

            _logger.LogInformation("Cliente creado: {Id} - {Nombre}", id, dto.NombreEmpresa);

            // Retornar el DTO con el ID asignado
            dto.Id = id;
            dto.FechaAlta = entity.FechaAlta;
            dto.Activo = true;

            return CreatedAtAction(nameof(ObtenerPorId), new { id }, dto);
        }

        /// <summary>
        /// Actualiza un cliente existente
        /// </summary>
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Actualizar cliente")]
        [SwaggerResponse(200, "Cliente actualizado", typeof(ClienteDto))]
        [SwaggerResponse(404, "Cliente no encontrado")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ClienteDto dto)
        {
            if (id != dto.Id)
            {
                return ErrorResponse("ID de ruta no coincide con ID de body");
            }

            var existente = await _clienteRepo.GetByIdAsync(id);
            if (existente == null)
            {
                return NotFound();
            }

            // Actualizar campos
            existente.NombreEmpresa = dto.NombreEmpresa;
            existente.CUIT = dto.CUIT;
            existente.Direccion = dto.Direccion;
            existente.ContactoNombre = dto.ContactoNombre;
            existente.ContactoEmail = dto.ContactoEmail;
            existente.ContactoTelefono = dto.ContactoTelefono;
            existente.Activo = dto.Activo;

            var exito = await _clienteRepo.UpdateAsync(existente);
            if (!exito)
            {
                return StatusCode(500, new { error = "Error actualizando cliente" });
            }

            return Ok(existente.ToDto());
        }

        /// <summary>
        /// Elimina un cliente (hard delete - según tu implementación)
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Eliminar cliente")]
        [SwaggerResponse(204, "Cliente eliminado")]
        [SwaggerResponse(404, "Cliente no encontrado")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var existente = await _clienteRepo.GetByIdAsync(id);
            if (existente == null)
            {
                return NotFound();
            }

            var exito = await _clienteRepo.DeleteAsync(id);
            if (!exito)
            {
                return StatusCode(500, new { error = "Error eliminando cliente" });
            }

            _logger.LogInformation("Cliente eliminado: {Id}", id);
            return NoContent();
        }
    }
}