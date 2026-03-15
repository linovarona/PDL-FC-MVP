//using FichaCosto.Repositories.Implementations;
//using FichaCosto.Repositories.Interfaces;
//using FichaCosto.Service.Models.Entities;
//using Microsoft.Data.Sqlite;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging.Abstractions;
//using System.Data;

using FichaCosto.Service.Models.Entities;
using FichaCosto.Repositories.Implementations;
using FichaCosto.Repositories.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using Xunit;
// Alias para resolver conflicto de nombres: namespace vs clase FichaCosto
using FichaCostoEntity = FichaCosto.Service.Models.Entities.FichaCosto;
namespace FichaCosto.Service.Tests
{
    public class RepositoryTests : IDisposable
    {
        private readonly string _connectionString;
        private readonly IDbConnection _connection;
        private readonly IClienteRepository _clienteRepo;
        private readonly IProductoRepository _productoRepo;
        private readonly IFichaRepository _fichaRepo;


        public RepositoryTests()
        {
            // Base de datos en memoria para tests
            _connectionString = "Data Source=:memory:";
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();

            // Crear schema - Buscar archivo en múltiples ubicaciones posibles
            string? schemaPath = FindSchemaFile();

            if (string.IsNullOrEmpty(schemaPath))
            {
                throw new FileNotFoundException("No se encontró Schema.sql. Buscado en: " +
                    string.Join(", ", GetPossibleSchemaPaths()));
            }

            var schema = File.ReadAllText(schemaPath);

            // Ejecutar schema comando por comando (SQLite no soporta múltiples statements en uno)
            var commands = schema.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                                 .Where(cmd => !string.IsNullOrWhiteSpace(cmd))
                                 .Select(cmd => cmd.Trim());

            foreach (var cmd in commands)
            {
                if (cmd.StartsWith("--") || string.IsNullOrWhiteSpace(cmd)) continue;

                using var command = new SqliteCommand(cmd + ";", (SqliteConnection)_connection);
                command.ExecuteNonQuery();
            }

            // Configuración mock
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString
                })
                .Build();

            _clienteRepo = new ClienteRepository(config, NullLogger<ClienteRepository>.Instance);
            _productoRepo = new ProductoRepository(config, NullLogger<ProductoRepository>.Instance);
            _fichaRepo = new FichaRepository(config, NullLogger<FichaRepository>.Instance);
        }

        private string? FindSchemaFile()
        {
            foreach (var path in GetPossibleSchemaPaths())
            {
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        private IEnumerable<string> GetPossibleSchemaPaths()
        {
            // Obtener directorio base del test
            var baseDir = AppContext.BaseDirectory;

            yield return Path.Combine(baseDir, "Data", "Schema.sql");
            yield return Path.Combine(baseDir, "..", "..", "..", "..", "FichaCosto.Service", "Data", "Schema.sql");
            yield return Path.Combine(baseDir, "..", "..", "..", "Data", "Schema.sql");
            yield return Path.Combine(Directory.GetCurrentDirectory(), "Data", "Schema.sql");

            // Ruta absoluta del proyecto
            yield return @"D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service\Data\Schema.sql";
        }
        //public RepositoryTests()
        //{
        //    // Base de datos en memoria para tests
        //    _connectionString = "Data Source=:memory:";
        //    _connection = new SqliteConnection(_connectionString);
        //    _connection.Open();

        //    // Crear schema
        //    var schema = File.ReadAllText("Data/Schema.sql");
        //    using var cmd = new SqliteCommand(schema, (SqliteConnection)_connection);
        //    cmd.ExecuteNonQuery();

        //    // Configuración mock
        //    var config = new ConfigurationBuilder()
        //        .AddInMemoryCollection(new Dictionary<string, string?>
        //        {
        //            ["ConnectionStrings:DefaultConnection"] = _connectionString
        //        })
        //        .Build();

        //    _clienteRepo = new ClienteRepository(config, NullLogger<ClienteRepository>.Instance);
        //    _productoRepo = new ProductoRepository(config, NullLogger<ProductoRepository>.Instance);
        //    _fichaRepo = new FichaRepository(config, NullLogger<FichaRepository>.Instance);
        //}

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }

        [Fact]
        public async Task ClienteRepository_Create_And_GetById()
        {
            // Arrange
            var cliente = new Cliente
            {
                NombreEmpresa = "Test Cliente",
                CUIT = "30111222333",
                Activo = true,
                FechaAlta = DateTime.Now
            };

            // Act
            var id = await _clienteRepo.CreateAsync(cliente);
            var result = await _clienteRepo.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cliente.NombreEmpresa, result.NombreEmpresa);
            Assert.Equal(cliente.CUIT, result.CUIT);
        }

        [Fact]
        public async Task ClienteRepository_ExistsByCuit_ReturnsTrue()
        {
            // Arrange
            var cliente = new Cliente
            {
                NombreEmpresa = "Cliente CUIT Test",
                CUIT = "30999888777",
                Activo = true,
                FechaAlta = DateTime.Now
            };
            await _clienteRepo.CreateAsync(cliente);

            // Act
            var exists = await _clienteRepo.ExistsByCuitAsync("30999888777");

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task ProductoRepository_Create_WithCliente()
        {
            // Arrange
            var cliente = new Cliente
            {
                NombreEmpresa = "Cliente Producto",
                CUIT = "30777666555",
                Activo = true,
                FechaAlta = DateTime.Now
            };
            var clienteId = await _clienteRepo.CreateAsync(cliente);

            var producto = new Producto
            {
                ClienteId = clienteId,
                Codigo = "PROD-TEST-001",
                Nombre = "Producto Test",
                UnidadMedida = Models.Enums.UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            // Act
            var id = await _productoRepo.CreateAsync(producto);
            var result = await _productoRepo.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(producto.Codigo, result.Codigo);
            Assert.Equal(clienteId, result.ClienteId);
        }

        [Fact]
        public async Task ProductoRepository_GetByClienteId_ReturnsProducts()
        {
            // Arrange
            var cliente = new Cliente
            {
                NombreEmpresa = "Cliente Multi",
                CUIT = "30555444333",
                Activo = true,
                FechaAlta = DateTime.Now
            };
            var clienteId = await _clienteRepo.CreateAsync(cliente);

            for (int i = 1; i <= 3; i++)
            {
                await _productoRepo.CreateAsync(new Producto
                {
                    ClienteId = clienteId,
                    Codigo = $"PROD-{i}",
                    Nombre = $"Producto {i}",
                    UnidadMedida = Models.Enums.UnidadMedida.Unidad,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                });
            }

            // Act
            var productos = await _productoRepo.GetByClienteIdAsync(clienteId);

            // Assert
            Assert.Equal(3, productos.Count());
        }

        [Fact]
        public async Task FichaRepository_Create_And_GetByProducto()
        {
            // Arrange
            var cliente = new Cliente
            {
                NombreEmpresa = "Cliente Ficha",
                CUIT = "30222111444",
                Activo = true,
                FechaAlta = DateTime.Now
            };
            var clienteId = await _clienteRepo.CreateAsync(cliente);

            var producto = new Producto
            {
                ClienteId = clienteId,
                Codigo = "PROD-FICHA",
                Nombre = "Producto Con Ficha",
                UnidadMedida = Models.Enums.UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.Now
            };
            var productoId = await _productoRepo.CreateAsync(producto);

            var ficha = new FichaCostoEntity
            {
                ProductoId = productoId,
                FechaCalculo = DateTime.Now,
                CostoMateriasPrimas = 100.50m,
                CostoManoObra = 50.25m,
                CostosDirectosTotales = 25.00m,
                //ToDo:GastosGenerales = 10.00m,
                //ToDo:CostoTotal = 185.75m,
                MargenUtilidad = 30.0m,
                //ToDo:PrecioVentaSugerido = 241.48m,
                EstadoValidacion = Models.Enums.EstadoValidacion.Valido,
                //ToDo:CalculadoPor = "TestUser"
            };

            // Act
            var id = await _fichaRepo.CreateAsync(ficha);
            var fichas = await _fichaRepo.GetByProductoIdAsync(productoId);

            // Assert
            Assert.Single(fichas);
            Assert.Equal(ficha.CostosDirectosTotales, fichas.First().CostosDirectosTotales);
            //ToDo:Assert.Equal(ficha.CostoTotal, fichas.First().CostoTotal);
        }
    }
}