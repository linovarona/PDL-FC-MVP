using FichaCosto.Service.Controllers;
using FichaCosto.Service.DTOs;
using FichaCosto.Service.Models.DTOs;
using FichaCosto.Service.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace FichaCosto.Service.Tests.Controllers
{
    public class FichasControllerIntegrationTests : ControllerIntegrationTestsBase
    {
        private readonly FichasController _controller;

        public FichasControllerIntegrationTests(ITestOutputHelper output) : base(output)
        {
            _controller = new FichasController(_fichaRepo, NullLogger<FichasController>.Instance);
        }

        [Fact]
        public async Task Crear_FichaValida_RetornaCreated()
        {
            var producto = await CrearProductoConDatosCompleto((await CrearClientePrueba()).Id);

            var resultado = new ResultadoCalculoDto
            {
                ProductoId = producto.Id,
                CostoMateriasPrimas = 155.00m,
                CostoManoObra = 2879.38m,
                CostosDirectosTotales = 3034.38m,
                MargenUtilidad = 30m,
                PrecioVentaCalculado = 3944.69m,
                EstadoValidacion = EstadoValidacion.Valido,
                //ObservacionesValidacion = "Válida",
                CostoTotal = 3034.38m,
                PrecioVentaSugerido = 3944.69m,
                CalculadoPor = "Test"
            };

            var result = await _controller.Crear(resultado);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var creada = Assert.IsType<ResultadoCalculoDto>(created.Value);

            Assert.True(creada.Id > 0);
            _output.WriteLine($"✓ Ficha creada: ID {creada.Id}");
        }

        [Fact]
        public async Task ObtenerPorId_Existente_RetornaOk()
        {
            var ficha = await CrearFichaPrueba();

            var result = await _controller.ObtenerPorId(ficha.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var obtenida = Assert.IsType<ResultadoCalculoDto>(ok.Value);
            Assert.Equal(ficha.Id, obtenida.Id);
            _output.WriteLine($"✓ Ficha {obtenida.Id} obtenida");
        }

        [Fact]
        public async Task ObtenerPorId_NoExistente_RetornaNotFound()
        {
            var result = await _controller.ObtenerPorId(99999);
            Assert.IsType<NotFoundResult>(result.Result);
            _output.WriteLine("✓ NotFound para ficha inexistente");
        }

        [Fact]
        public async Task ObtenerHistorial_ProductoConFichas_RetornaOrdenado()
        {
            var producto = await CrearProductoConDatosCompleto((await CrearClientePrueba()).Id);

            // Crear 3 fichas con delays para diferenciar fechas
            await CrearFichaPrueba(producto.Id, 25m);
            await Task.Delay(50);
            await CrearFichaPrueba(producto.Id, 30m);
            await Task.Delay(50);
            await CrearFichaPrueba(producto.Id, 20m);

            var result = await _controller.ObtenerHistorial(producto.Id, 10);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var historial = Assert.IsType<List<ResultadoCalculoDto>>(ok.Value);

            Assert.Equal(3, historial.Count);
            // Verificar orden descendente
            Assert.True(historial[0].FechaCalculo >= historial[1].FechaCalculo);
            _output.WriteLine($"✓ Historial: {historial.Count} fichas ordenadas");
        }

        [Fact]
        public async Task ObtenerHistorial_RespetaLimite()
        {
            var producto = await CrearProductoConDatosCompleto((await CrearClientePrueba()).Id);

            for (int i = 0; i < 5; i++)
            {
                await CrearFichaPrueba(producto.Id, 25m);
            }

            var result = await _controller.ObtenerHistorial(producto.Id, 3);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var historial = Assert.IsType<List<ResultadoCalculoDto>>(ok.Value);
            Assert.Equal(3, historial.Count);
            _output.WriteLine("✓ Límite de 3 fichas respetado");
        }

        [Fact]
        public async Task ObtenerUltima_ProductoConFichas_RetornaMasReciente()
        {
            var producto = await CrearProductoConDatosCompleto((await CrearClientePrueba()).Id);

            await CrearFichaPrueba(producto.Id, 25m);
            await Task.Delay(50);
            var reciente = await CrearFichaPrueba(producto.Id, 30m);

            var result = await _controller.ObtenerUltima(producto.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var ultima = Assert.IsType<ResultadoCalculoDto>(ok.Value);

            Assert.Equal(reciente.Id, ultima.Id);
            Assert.Equal(30m, ultima.MargenUtilidad);
            _output.WriteLine("✓ Última ficha es la más reciente");
        }

        [Fact]
        public async Task ObtenerUltima_ProductoSinFichas_RetornaNotFound()
        {
            var producto = await CrearProductoPrueba();

            var result = await _controller.ObtenerUltima(producto.Id);

            Assert.IsType<NotFoundResult>(result.Result);
            _output.WriteLine("✓ NotFound para producto sin fichas");
        }

        // Helper
        private async Task<ResultadoCalculoDto> CrearFichaPrueba(int? productoId = null, decimal margen = 30m)
        {
            var pid = productoId ?? (await CrearProductoConDatosCompleto((await CrearClientePrueba()).Id)).Id;

            var resultado = new ResultadoCalculoDto
            {
                ProductoId = pid,
                CostoMateriasPrimas = 100m,
                CostoManoObra = 200m,
                CostosDirectosTotales = 300m,
                MargenUtilidad = margen,
                PrecioVentaCalculado = 300m * (1 + margen / 100),
                EstadoValidacion = margen <= 30 ? EstadoValidacion.Valido : EstadoValidacion.Excedido,
                CostoTotal = 300m,
                PrecioVentaSugerido = 300m * (1 + margen / 100),
                CalculadoPor = "Test"
            };

            var result = await _controller.Crear(resultado);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            return (ResultadoCalculoDto)created.Value!;
        }
    }
}
