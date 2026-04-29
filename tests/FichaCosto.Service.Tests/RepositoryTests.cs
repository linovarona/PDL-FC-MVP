using Dapper;
using FichaCosto.Repositories.Implementations;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Models.Entities;
using FichaCosto.Service.Models.Enums;
//using FichaCosto.Tests.Helpers;

using FichaCostoEntity = FichaCosto.Service.Models.Entities.FichaCosto;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using Xunit;
using Xunit.Abstractions;

namespace FichaCosto.Service.Tests
{
    public class RepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly IClienteRepository _clienteRepo;
        private readonly IProductoRepository _productoRepo;
        private readonly IFichaRepository _fichaRepo;

        public RepositoryTests()
        {
            // SQLite en memoria con conexión compartida
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            // Crear schema
            var schema = File.ReadAllText("Data/Schema.sql");
            _connection.Execute(schema);

            // Factory y repositorios
            var factory = new TestConnectionFactory(_connection);

            _clienteRepo = new ClienteRepository(factory, NullLogger<ClienteRepository>.Instance);
            _productoRepo = new ProductoRepository(factory, NullLogger<ProductoRepository>.Instance);
            _fichaRepo = new FichaRepository(factory, NullLogger<FichaRepository>.Instance);
        }

        [Fact]
        public async Task Cliente_CRUD()
        {
            var cliente = new Cliente
            {
                NombreEmpresa = "Test S.A.",
                CUIT = "30111222333",
                ContactoTelefono = "555-0100",
                ContactoEmail = "test@test.com",
                Activo = true,
                FechaAlta = DateTime.UtcNow
            };

            var id = await _clienteRepo.CreateAsync(cliente);
            Assert.True(id > 0);

            var leido = await _clienteRepo.GetByIdAsync(id);
            Assert.NotNull(leido);
            Assert.Equal(cliente.NombreEmpresa, leido.NombreEmpresa);

            leido.NombreEmpresa = "Modificado";
            Assert.True(await _clienteRepo.UpdateAsync(leido));

            Assert.True(await _clienteRepo.ExistsByCuitAsync(cliente.CUIT));
            Assert.True(await _clienteRepo.DeleteAsync(id));
            Assert.Null(await _clienteRepo.GetByIdAsync(id));
        }

        [Fact]
        public async Task Producto_ConDetalles()
        {
            var clienteId = await _clienteRepo.CreateAsync(new Cliente
            {
                NombreEmpresa = "Cliente",
                CUIT = "30777666555",
                Activo = true,
                FechaAlta = DateTime.UtcNow
            });

            var productoId = await _productoRepo.CreateAsync(new Producto
            {
                ClienteId = clienteId,
                Codigo = "P001",
                Nombre = "Producto",
                UnidadMedida = UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });

            var conDetalles = await _productoRepo.GetByIdWithDetailsAsync(productoId);
            Assert.NotNull(conDetalles);
            Assert.NotNull(conDetalles.MateriasPrimas);
        }

        [Fact]
        public async Task Ficha_SinCamposNoHabilitados()
        {
            // Setup
            var clienteId = await _clienteRepo.CreateAsync(new Cliente
            {
                NombreEmpresa = "Ficha Test",
                CUIT = "30222111444",
                Activo = true,
                FechaAlta = DateTime.UtcNow
            });

            var productoId = await _productoRepo.CreateAsync(new Producto
            {
                ClienteId = clienteId,
                Codigo = "F001",
                Nombre = "Prod Ficha",
                UnidadMedida = UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });

            // Ficha SIN CostosIndirectos y GastosGenerales
            var ficha = new FichaCostoEntity
            {
                ProductoId = productoId,
                FechaCalculo = DateTime.UtcNow,
                CostoMateriasPrimas = 100m,
                CostoManoObra = 50m,
                //CostoTotal = 150m,
                MargenUtilidad = 30m,
                //PrecioVentaSugerido = 195m,
                EstadoValidacion = EstadoValidacion.Valido,
                //CalculadoPor = "Test"
            };

            var id = await _fichaRepo.CreateAsync(ficha);
            Assert.True(id > 0);

            var historial = await _fichaRepo.GetHistorialByProductoIdAsync(productoId);
            Assert.Single(historial);
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}