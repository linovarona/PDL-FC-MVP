using FichaCosto.Service.Controllers;
using FichaCosto.Service.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FichaCosto.Service.Tests.Controllers
{
    public class ConfiguracionControllerIntegrationTests
    {
        private readonly ConfiguracionController _controller;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<ILogger<ConfiguracionController>> _loggerMock;

        public ConfiguracionControllerIntegrationTests()
        {
            _configMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<ConfiguracionController>>();

            _configMock.Setup(x => x["ASPNETCORE_ENVIRONMENT"]).Returns("Development");

            _controller = new ConfiguracionController(_configMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task ObtenerConfiguracion_RetornaEstructuraCorrecta()
        {
            // Act
            var result = _controller.ObtenerConfiguracion();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var config = Assert.IsType<ConfiguracionSistemaDto>(okResult.Value);

            // Verificar estructura 'calculo'
            Assert.NotNull(config.Calculo);
            Assert.Equal(30.0, config.Calculo.MargenUtilidadMaximo);
            Assert.Equal(2, config.Calculo.DecimalesRedondeo);
            Assert.Equal("HorasMaquina", config.Calculo.MetodoRepartoDefault);

            // Verificar estructura 'resolucion'
            Assert.NotNull(config.Resolucion);
            Assert.Equal("148/2023", config.Resolucion.Metodologia);
            Assert.Equal("209/2024", config.Resolucion.Margen);
            Assert.Equal("2024-07-01", config.Resolucion.FechaVigenciaMargen);

            // Verificar estructura 'api'
            Assert.NotNull(config.Api);
            Assert.Equal("v1", config.Api.Version);
            Assert.Equal("Development", config.Api.Entorno);
        }

        [Fact]
        public void ObtenerResoluciones_RetornaListaCorrecta()
        {
            // Act
            var result = _controller.ObtenerResoluciones();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resoluciones = Assert.IsType<List<ResolucionInfoDto>>(okResult.Value);

            Assert.Equal(2, resoluciones.Count);

            // Primera resolución (148/2023)
            var res148 = resoluciones[0];
            Assert.Equal("148/2023", res148.Numero);
            Assert.Equal("Metodología de Costos", res148.Nombre);
            Assert.Null(res148.MargenMaximo); // No tiene margen

            // Segunda resolución (209/2024)
            var res209 = resoluciones[1];
            Assert.Equal("209/2024", res209.Numero);
            Assert.Equal("Márgenes de Utilidad", res209.Nombre);
            Assert.Equal(30.0, res209.MargenMaximo);
        }

        [Fact]
        public void ObtenerUnidadesMedida_RetornaTodasLasUnidades()
        {
            // Act
            var result = _controller.ObtenerUnidadesMedida();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var unidades = Assert.IsType<List<UnidadMedidaDto>>(okResult.Value);

            Assert.True(unidades.Count > 0);
            Assert.Contains(unidades, u => u.Nombre == "Kilogramo");
            Assert.Contains(unidades, u => u.Codigo > 0);
        }

        [Fact]
        public void ObtenerMetodosReparto_RetornaTresMetodos()
        {
            // Act
            var result = _controller.ObtenerMetodosReparto();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var metodos = Assert.IsType<List<MetodoRepartoDto>>(okResult.Value);

            Assert.Equal(3, metodos.Count);
            Assert.Contains(metodos, m => m.Codigo == "HorasMaquina");
            Assert.Contains(metodos, m => m.Codigo == "ValorProduccion");
            Assert.Contains(metodos, m => m.Codigo == "UnidadesProducidas");
        }
    }
}