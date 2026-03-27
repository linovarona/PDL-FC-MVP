using Dapper;
using FichaCosto.Repositories.Implementations;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.DTOs;
using FichaCosto.Service.Mappings;
using FichaCosto.Service.Models.Entities;
using FichaCosto.Service.Services.Implementations;
using FichaCosto.Service.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using Xunit.Abstractions;

namespace FichaCosto.Service.Tests.Controllers
{
    /// <summary>
    /// Base para tests de integración de Controllers usando SQLite en memoria
    /// Patrón consistente con RepositorySharedTests
    /// </summary>
    public abstract class ControllerIntegrationTestsBase : IDisposable
    {
        protected readonly ITestOutputHelper _output;
        protected readonly SqliteConnection _sharedConnection;
        protected readonly IConnectionFactory _connectionFactory;

        // Repositorios
        protected readonly IClienteRepository _clienteRepo;
        protected readonly IProductoRepository _productoRepo;
        protected readonly IFichaRepository _fichaRepo;

        // Servicios de negocio (Fase 4)
        protected readonly ICalculadoraCostoService _calculadoraService;
        protected readonly IValidadorFichaService _validadorService;
        protected readonly IExcelService _excelService;

        protected ControllerIntegrationTestsBase(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] === INICIO TEST CONTROLLER ===");

            // Crear y abrir conexión SQLite en memoria
            _sharedConnection = new SqliteConnection("Data Source=:memory:");
            _sharedConnection.Open();

            _output.WriteLine($"Conexión abierta. Hash: {_sharedConnection.GetHashCode()}");

            // Ejecutar schema
            EjecutarSchema();

            // Crear factory
            _connectionFactory = new TestConnectionFactory(_sharedConnection);

            // Crear repositorios
            _clienteRepo = new ClienteRepository(
                _connectionFactory,
                NullLogger<ClienteRepository>.Instance);

            _productoRepo = new ProductoRepository(
                _connectionFactory,
                NullLogger<ProductoRepository>.Instance);

            _fichaRepo = new FichaRepository(
                _connectionFactory,
                NullLogger<FichaRepository>.Instance);

            // Crear servicios de Fase 4
            _validadorService = new ValidadorFichaService( //); Yo
            NullLogger<ValidadorFichaService>.Instance);


            _calculadoraService = new CalculadoraCostoService(
              //_clienteRepo, Yo
                _productoRepo, 
              /*_fichaRepo, Yo*/
                _validadorService,
                NullLogger<CalculadoraCostoService>.Instance);


            _excelService = new ExcelService(
                //_clienteRepo,
                //_productoRepo,
                NullLogger<ExcelService>.Instance);

            _output.WriteLine("Repositorios y servicios inicializados");
        }

        private void EjecutarSchema()
        {
            var schemaPath = BuscarSchemaSql();
            var schemaSql = File.ReadAllText(schemaPath);

            using var cmd = new SqliteCommand(schemaSql, _sharedConnection);
            cmd.ExecuteNonQuery();

            _output.WriteLine($"Schema ejecutado");
        }

        private string BuscarSchemaSql()
        {
            var rutas = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "Schema.sql"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FichaCosto.Service", "Data", "Schema.sql"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "Schema.sql"),
                @"D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service\Data\Schema.sql"
            };

            foreach (var ruta in rutas.Select(Path.GetFullPath))
            {
                if (File.Exists(ruta))
                {
                    _output.WriteLine($"Schema: {ruta}");
                    return ruta;
                }
            }

            throw new FileNotFoundException("Schema.sql no encontrado");
        }

        // ==================== HELPERS ====================

        protected async Task<ClienteDto> CrearClientePrueba(string cuitSuffix = "")
        {
            var cliente = new Cliente
            {
                NombreEmpresa = $"PyME Test {Guid.NewGuid()}",
                CUIT = $"30{new Random().Next(100000000, 999999999)}{cuitSuffix}",
                Direccion = "Av. Test 123",
                ContactoNombre = "Juan Test",
                ContactoEmail = "test@pyme.com",
                ContactoTelefono = "555-1234",
                Activo = true,
                FechaAlta = DateTime.UtcNow
            };

            var id = await _clienteRepo.CreateAsync(cliente);
            var creado = await _clienteRepo.GetByIdAsync(id);
            return creado!.ToDto();
        }

        protected async Task<ProductoDto> CrearProductoPrueba(int? clienteId = null)
        {
            var cliente = clienteId.HasValue
                ? await _clienteRepo.GetByIdAsync(clienteId.Value)
                : await _clienteRepo.GetByIdAsync((await CrearClientePrueba()).Id);

            var producto = new Producto
            {
                ClienteId = cliente!.Id,
                Codigo = $"PROD-{Guid.NewGuid().ToString()[..8]}",
                Nombre = $"Producto Test {Guid.NewGuid()}",
                Descripcion = "Descripción de prueba",
                UnidadMedida = Models.Enums.UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var id = await _productoRepo.CreateAsync(producto);
            var creado = await _productoRepo.GetByIdAsync(id);
            return creado!.ToDto();
        }

        protected async Task<Producto> CrearProductoConDatosCompleto(int clienteId)
        {
            // Crear producto
            var producto = new Producto
            {
                ClienteId = clienteId,
                Codigo = $"PROD-{Guid.NewGuid().ToString()[..8]}",
                Nombre = $"Producto Completo {Guid.NewGuid()}",
                Descripcion = "Producto con MP y MO",
                UnidadMedida = Models.Enums.UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var productoId = await _productoRepo.CreateAsync(producto);

            // Agregar materias primas directamente a la BD
            using var connection = _connectionFactory.CreateConnection();
            const string sqlMP = @"
                INSERT INTO MateriasPrimas (ProductoId, Nombre, Cantidad, CostoUnitario, Orden, Activo)
                VALUES (@ProductoId, @Nombre, @Cantidad, @CostoUnitario, @Orden, 1);
                SELECT last_insert_rowid();";

            var mp1Id = await connection.ExecuteScalarAsync<int>(sqlMP, new
            {
                ProductoId = productoId,
                Nombre = "Materia Prima A",
                //Cantidad = 10.5m,
                Cantidad = 10,
                //CostoUnitario = 15.50m,
                CostoUnitario = 15,

                Orden = 1
            });

            var mp2Id = await connection.ExecuteScalarAsync<int>(sqlMP, new
            {
                ProductoId = productoId,
                Nombre = "Materia Prima B",
                //Cantidad = 5.0m,
                Cantidad = 5,
                CostoUnitario = 25.00m,
                Orden = 2
            });

            // Agregar mano de obra
            const string sqlMO = @"
                INSERT INTO ManoObraDirecta (ProductoId, Horas, SalarioHora, PorcentajeCargasSociales, DescripcionTarea)
                VALUES (@ProductoId, @Horas, @SalarioHora, @PorcentajeCargasSociales, @DescripcionTarea);";

            await connection.ExecuteAsync(sqlMO, new
            {
                ProductoId = productoId,
                Horas = 2.5m,
                SalarioHora = 850.00m,
                PorcentajeCargasSociales = 35.5m,
                DescripcionTarea = "Ensamblaje"
            });

            return (await _productoRepo.GetByIdWithDetailsAsync(productoId))!;
        }

        public void Dispose()
        {
            _output.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] === LIMPIEZA ===");

            if (_sharedConnection?.State == ConnectionState.Open)
            {
                _sharedConnection.Close();
                _output.WriteLine("Conexión cerrada. BD destruida.");
            }

            _sharedConnection?.Dispose();
            _output.WriteLine("=== FIN TEST ===");
        }
    }
}
