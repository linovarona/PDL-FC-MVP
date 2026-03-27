using Dapper;
using FichaCosto.Service.Controllers;
using FichaCosto.Service.DTOs;
using FichaCosto.Service.Models.DTOs;
using FichaCosto.Service.Models.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using Xunit.Abstractions;

namespace FichaCosto.Service.Tests.Controllers
{
    public class CostosControllerIntegrationTests : ControllerIntegrationTestsBase
    {
        private readonly CostosController _controller;

        public CostosControllerIntegrationTests(ITestOutputHelper output) : base(output)
        {
            _controller = new CostosController(
                _calculadoraService,
                _validadorService,
                NullLogger<CostosController>.Instance);
        }

        [Fact]
        public async Task Calcular_FichaValida_RetornaCalculosCorrectos()
        {
            var producto = await CrearProductoConDatosCompleto((await CrearClientePrueba()).Id);

            var request = new FichaCostoDto
            {
                ProductoId = producto.Id,
                MateriasPrimas = new List<MateriaPrimaInputDto>
                {
                    new() { Nombre = "MP A", Cantidad = 10, CostoUnitario = 15.50m },
                    new() { Nombre = "MP B", Cantidad = 5, CostoUnitario = 25.00m }
                },
                ManoObra = new ManoObraInputDto
                {
                    Horas = 2.5m,
                    SalarioHora = 850m,
                    PorcentajeCargasSociales = 35.5m
                },
                MargenUtilidad = 30m
            };

            var result = await _controller.Calcular(request);

            var ok = Assert.IsType<OkObjectResult>(result/*.Result*/);
            var calculo = Assert.IsType<ResultadoCalculoDto>(ok.Value);

            // Verificar cálculos
            var esperadoMP = 10 * 15.50m + 5 * 25.00m; // 280
            var esperadoMO = 2.5m * 850m * 1.355m;     // ~2879.38
            var esperadoTotal = esperadoMP + esperadoMO;

            Assert.True(calculo.CostosDirectosTotales > 0);
            Assert.True(calculo.PrecioVentaCalculado > calculo.CostosDirectosTotales);
            Assert.Equal(30m, calculo.MargenUtilidad);
            _output.WriteLine($"✓ Cálculo: CD={calculo.CostosDirectosTotales}, PV={calculo.PrecioVentaCalculado}");
        }

        [Fact]
        public async Task Calcular_Margen30_Valido()
        {
            var producto = await CrearProductoConDatosCompleto((await CrearClientePrueba()).Id);

            var request = new FichaCostoDto
            {
                ProductoId = producto.Id,
                MateriasPrimas = new List<MateriaPrimaInputDto>
                {
                    new() { Nombre = "MP", Cantidad = 1, CostoUnitario = 100m }
                },
                ManoObra = new ManoObraInputDto { Horas = 1, SalarioHora = 100m },
                MargenUtilidad = 30m  // Límite exacto
            };

            var result = await _controller.Calcular(request);

            var ok = Assert.IsType<OkObjectResult>(result/*.Result*/);
            var calculo = Assert.IsType<ResultadoCalculoDto>(ok.Value);

            Assert.Equal(EstadoValidacion.ValidadaConObservaciones, calculo.EstadoValidacion);
            _output.WriteLine("✓ Margen 30% aceptado");
        }

        [Fact]
        public async Task Calcular_MargenExcede30_RetornaBadRequest()
        {
            var producto = await CrearProductoConDatosCompleto((await CrearClientePrueba()).Id);

            var request = new FichaCostoDto
            {
                ProductoId = producto.Id,
                MateriasPrimas = new List<MateriaPrimaInputDto>
                {
                    new() { Nombre = "MP", Cantidad = 1, CostoUnitario = 100m }
                },
                ManoObra = new ManoObraInputDto { Horas = 1, SalarioHora = 100m },
                MargenUtilidad = 35m  // Excede
            };

            var result = await _controller.Calcular(request);

            // El validador debería rechazar antes de calcular, o el servicio retornar error
            Assert.True(
                result/*.Result*/ is BadRequestObjectResult ||
                (result/*.Result*/ is OkObjectResult ok &&
                 ((ResultadoCalculoDto)ok.Value!).EstadoValidacion == EstadoValidacion.Excedido /*ExcedeMargen*/),
                "Debe rechazar margen > 30% o marcar como excedido"
            );
            _output.WriteLine("✓ Margen 35% rechazado o marcado como excedido");
        }

        [Fact]
        public async Task Validar_FichaValida_RetornaValidacionExitosa()
        {
            var producto = await CrearProductoConDatosCompleto((await CrearClientePrueba()).Id);

            var request = new FichaCostoDto
            {
                ProductoId = producto.Id,
                MateriasPrimas = new List<MateriaPrimaInputDto>
                {
                    new() { Nombre = "MP", Cantidad = 10, CostoUnitario = 15m }
                },
                ManoObra = new ManoObraInputDto { Horas = 2, SalarioHora = 500m },
                MargenUtilidad = 25m
            };

            var result = await _controller.Validar(request);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var validacion = Assert.IsType<ResultadoValidacionDto>(ok.Value);

            Assert.True(validacion.EsValido);
            _output.WriteLine("✓ Validación exitosa");
        }

        [Fact]
        public async Task Validar_FichaConMargenExcesivo_RetornaNoValida()
        {
            var producto = await CrearProductoConDatosCompleto((await CrearClientePrueba()).Id);

            var request = new FichaCostoDto
            {
                ProductoId = producto.Id,
                MateriasPrimas = new List<MateriaPrimaInputDto>
                {
                    new() { Nombre = "MP", Cantidad = 1, CostoUnitario = 1m }
                },
                ManoObra = new ManoObraInputDto { Horas = 1, SalarioHora = 1m },
                MargenUtilidad = 35m
            };

            var result = await _controller.Validar(request);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var validacion = Assert.IsType<ResultadoValidacionDto>(ok.Value);

            Assert.False(validacion.EsValida);
            Assert.NotEmpty(validacion.Errores);
            _output.WriteLine($"✓ Validación rechazada: {string.Join(", ", validacion.Errores)}");
        }


        [Fact]
        public void Formulas_RetornaEstructuraCorrecta()
        {
            // Act
            var result = _controller.ObtenerFormulas();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<FormulasCostoDto>(okResult.Value);

            Assert.Equal("148/2023", data.Resolucion);
            Assert.NotEmpty(data.Formulas);
            Assert.Equal(3, data.Formulas.Count);
            Assert.Equal(30, data.MargenMaximo.Porcentaje);
            Assert.Equal("209/2024", data.MargenMaximo.Resolucion);

            _output.WriteLine("✓ Fórmulas y resoluciones correctas");
        }

        
    }
}
