using FichaCosto.Service.Controllers;
using FichaCosto.Service.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace FichaCosto.Service.Tests.Controllers
{
    public class ClientesControllerIntegrationTests : ControllerIntegrationTestsBase
    {
        private readonly ClientesController _controller;

        public ClientesControllerIntegrationTests(ITestOutputHelper output) : base(output)
        {
            _controller = new ClientesController(_clienteRepo, NullLogger<ClientesController>.Instance);
        }

        [Fact]
        public async Task ObtenerTodos_SinClientes_RetornaListaVacia()
        {
            var result = await _controller.ObtenerTodos();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var clientes = Assert.IsAssignableFrom<IEnumerable<ClienteDto>>(okResult.Value).ToList();
            Assert.Empty(clientes);
            _output.WriteLine("✓ Lista vacía inicial");
        }

        [Fact]
        public async Task ObtenerTodos_ConClientes_RetornaLista()
        {
            await CrearClientePrueba("");
            await CrearClientePrueba("");

            var result = await _controller.ObtenerTodos();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var clientes = Assert.IsAssignableFrom<IEnumerable<ClienteDto>>(okResult.Value).ToList();
            Assert.Equal(2, clientes.Count);
            _output.WriteLine($"✓ {clientes.Count} clientes retornados");
        }

        [Fact]
        public async Task Crear_ClienteValido_RetornaCreated()
        {
            var nuevo = new ClienteDto
            {
                NombreEmpresa = "PyME Nueva S.A.",
                CUIT = "30999888777",
                Direccion = "Av. Nueva 123",
                ContactoNombre = "Nuevo Contacto",
                ContactoEmail = "nuevo@pyme.com",
                ContactoTelefono = "555-9999",
                Activo = true
            };

            var result = await _controller.Crear(nuevo);

            var created = Assert.IsType<CreatedAtActionResult>(result/*.Result Yo*/);
            var creado = Assert.IsType<ClienteDto>(created.Value);

            Assert.True(creado.Id > 0);
            Assert.Equal(nuevo.NombreEmpresa, creado.NombreEmpresa);
            Assert.True(creado.FechaAlta > DateTime.MinValue);
            _output.WriteLine($"✓ Cliente creado: ID {creado.Id}");
        }

        [Fact]
        public async Task Crear_CUITDuplicado_RetornaBadRequest()
        {
            var existente = await CrearClientePrueba();

            var duplicado = new ClienteDto
            {
                NombreEmpresa = "Otra PyME",
                CUIT = existente.CUIT,
                Activo = true
            };

            var result = await _controller.Crear(duplicado);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result/*.Result Yo*/);
            _output.WriteLine("✓ Validación CUIT duplicado");
        }

        [Fact]
        public async Task ObtenerPorId_Existente_RetornaOk()
        {
            var creado = await CrearClientePrueba();

            var result = await _controller.ObtenerPorId(creado.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var cliente = Assert.IsType<ClienteDto>(ok.Value);
            Assert.Equal(creado.Id, cliente.Id);
            _output.WriteLine($"✓ Cliente {cliente.Id} recuperado");
        }

        [Fact]
        public async Task ObtenerPorId_NoExistente_RetornaNotFound()
        {
            var result = await _controller.ObtenerPorId(99999);
            Assert.IsType<NotFoundObjectResult>(result.Result);
            _output.WriteLine("✓ NotFound para ID inexistente");
        }

        [Fact]
        public async Task Actualizar_Existente_RetornaOk()
        {
            var creado = await CrearClientePrueba();
            creado.NombreEmpresa = "Actualizado";

            var result = await _controller.Actualizar(creado.Id, creado);

            var ok = Assert.IsType<OkObjectResult>(result/*.Result*/);
            var actualizado = Assert.IsType<ClienteDto>(ok.Value);
            Assert.Equal("Actualizado", actualizado.NombreEmpresa);

            var desdeBD = await _clienteRepo.GetByIdAsync(creado.Id);
            Assert.Equal("Actualizado", desdeBD?.NombreEmpresa);
            _output.WriteLine("✓ Actualización persistida");
        }

        [Fact]
        public async Task Actualizar_IdNoCoincide_RetornaBadRequest()
        {
            var creado = await CrearClientePrueba();

            var result = await _controller.Actualizar(999, creado);

            Assert.IsType<BadRequestObjectResult>(result/*Result*/);
            _output.WriteLine("✓ Validación ID no coincide");
        }

        [Fact]
        public async Task Eliminar_Existente_RetornaNoContent()
        {
            var creado = await CrearClientePrueba();

            var result = await _controller.Eliminar(creado.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _clienteRepo.GetByIdAsync(creado.Id));
            _output.WriteLine("✓ Eliminación confirmada");
        }

        [Fact]
        public async Task Eliminar_NoExistente_RetornaNotFound()
        {
            var result = await _controller.Eliminar(99999);
            Assert.IsType<NotFoundResult>(result);
            _output.WriteLine("✓ NotFound al eliminar inexistente");
        }
    }
}
