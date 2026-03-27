using FichaCosto.Service.Controllers;
using FichaCosto.Service.DTOs;
using FichaCosto.Service.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace FichaCosto.Service.Tests.Controllers
{
    public class ProductosControllerIntegrationTests : ControllerIntegrationTestsBase
    {
        private readonly ProductosController _controller;

        public ProductosControllerIntegrationTests(ITestOutputHelper output) : base(output)
        {
            _controller = new ProductosController(
                _productoRepo,
                NullLogger<ProductosController>.Instance);
        }

        [Fact]
        public async Task ObtenerPorCliente_SinProductos_RetornaVacia()
        {
            var cliente = await CrearClientePrueba();

            var result = await _controller.ObtenerPorCliente(cliente.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var productos = Assert.IsType<List<ProductoDto>>(ok.Value);

            Assert.Empty(productos);
            _output.WriteLine("✓ Lista vacía para cliente nuevo");
        }

        [Fact]
        public async Task Crear_ProductoValido_RetornaCreated()
        {
            var cliente = await CrearClientePrueba();
            var nuevo = new ProductoDto
            {
                ClienteId = cliente.Id,
                Codigo = "PROD-001",
                Nombre = "Producto Test",
                Descripcion = "Descripción",
                UnidadMedida = Models.Enums.UnidadMedida.Unidad,
                Activo = true
            };

            var result = await _controller.Crear(nuevo);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var creado = Assert.IsType<ProductoDto>(created.Value);

            Assert.True(creado.Id > 0);
            Assert.Equal(nuevo.Codigo, creado.Codigo);
            _output.WriteLine($"✓ Producto creado: ID {creado.Id}");
        }

        [Fact]
        public async Task Crear_CodigoDuplicado_RetornaBadRequest()
        {
            var cliente = await CrearClientePrueba();
            var p1 = new ProductoDto
            {
                ClienteId = cliente.Id,
                Codigo = "DUP-001",
                Nombre = "Producto 1",
                UnidadMedida = Models.Enums.UnidadMedida.Unidad
            };
            await _controller.Crear(p1);

            var p2 = new ProductoDto
            {
                ClienteId = cliente.Id,
                Codigo = "DUP-001",
                Nombre = "Producto 2",
                UnidadMedida = Models.Enums.UnidadMedida.Unidad
            };

            var result = await _controller.Crear(p2);
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _output.WriteLine("✓ Validación código duplicado");
        }

        [Fact]
        public async Task ObtenerPorId_Existente_RetornaCompleto()
        {
            var producto = await CrearProductoConDatosCompleto((await CrearClientePrueba()).Id);

            var result = await _controller.ObtenerPorId(producto.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var completo = Assert.IsType<ProductoCompletoDto>(ok.Value);

            Assert.NotNull(completo.Producto);
            Assert.NotNull(completo.MateriasPrimas);
            Assert.True(completo.MateriasPrimas.Any() || completo.ManoObra != null);
            _output.WriteLine($"✓ Producto {producto.Id} con detalles");
        }

        [Fact]
        public async Task Actualizar_Existente_RetornaOk()
        {
            var producto = await CrearProductoPrueba();
            producto.Nombre = "Actualizado";

            var result = await _controller.Actualizar(producto.Id, producto);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var actualizado = Assert.IsType<ProductoDto>(ok.Value);
            Assert.Equal("Actualizado", actualizado.Nombre);
            _output.WriteLine("✓ Producto actualizado");
        }

        [Fact]
        public async Task Eliminar_Existente_RetornaNoContent()
        {
            var producto = await CrearProductoPrueba();

            var result = await _controller.Eliminar(producto.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _productoRepo.GetByIdAsync(producto.Id));
            _output.WriteLine("✓ Producto eliminado");
        }

        //[Fact]
        //public async Task AgregarMateriaPrima_ProductoExistente_RetornaCreated()
        //{
        //    var producto = await CrearProductoPrueba();
        //    var mp = new MateriaPrimaDto
        //    {
        //        Nombre = "Madera Roble",
        //        CodigoInterno = "MP-001",
        //        Cantidad = 10.5m,
        //        CostoUnitario = 25.75m,
        //        Orden = 1,
        //        Activo = true
        //    };

        //    var result = await _controller.AgregarMateriaPrima(producto.Id, mp);

        //    var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        //    var creada = Assert.IsType<MateriaPrimaDto>(created.Value);

        //    Assert.True(creada.Id > 0);
        //    Assert.Equal(mp.Nombre, creada.Nombre);
        //    Assert.Equal(10.5m * 25.75m, creada.CostoTotal);
        //    _output.WriteLine($"✓ Materia prima creada: ID {creada.Id}");
        //}

        //[Fact]
        //public async Task ObtenerMateriasPrimas_ProductoConMaterias_RetornaLista()
        //{
        //    var producto = await CrearProductoConDatosCompleto((await CrearClientePrueba()).Id);

        //    var result = await _controller.ObtenerMateriasPrimas(producto.Id);

        //    var ok = Assert.IsType<OkObjectResult>(result.Result);
        //    var materias = Assert.IsType<List<MateriaPrimaDto>>(ok.Value);
        //    Assert.NotEmpty(materias);
        //    _output.WriteLine($"✓ {materias.Count} materias primas");
        //}

        //[Fact]
        //public async Task ActualizarManoObra_ProductoExistente_CreaNueva()
        //{
        //    var producto = await CrearProductoPrueba();
        //    var mo = new ManoObraDirecta
        //    {
        //        ProductoId = producto.Id,
        //        Horas = 2.5m,
        //        SalarioHora = 850.00m,
        //        PorcentajeCargasSociales = 35.5m,
        //        DescripcionTarea = "Ensamblaje"
        //    };

        //    var result = await _controller.ActualizarManoObra(producto.Id, mo);

        //    var ok = Assert.IsType<OkObjectResult>(result.Result);
        //    _output.WriteLine("✓ Mano de obra creada/actualizada");
        //}

        //[Fact]
        //public async Task ObtenerManoObra_SinManoObra_RetornaNotFound()
        //{
        //    var producto = await CrearProductoPrueba();

        //    var result = await _controller.ObtenerManoObra(producto.Id);

        //    Assert.IsType<NotFoundResult>(result.Result);
        //    _output.WriteLine("✓ NotFound para MO inexistente");
        //}
    }
}
