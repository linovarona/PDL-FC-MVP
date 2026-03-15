using Dapper;
using FichaCosto.Repositories.Implementations;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Models.Entities;
using FichaCosto.Service.Models.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using FichaCostoEntity = FichaCosto.Service.Models.Entities.FichaCosto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace FichaCosto.Service.Tests
{
    /// <summary>
    /// Tests de repositorios usando SQLite Shared Cache.
    /// 
    /// PROBLEMA QUE RESUELVE:
    /// SQLite en memoria (:memory:) crea una BD NUEVA por cada conexión.
    /// Si el Repository cierra la conexión con 'using', la BD desaparece.
    /// 
    /// SOLUCIÓN - Shared Cache:
    /// "Data Source=:memory:;Cache=Shared" permite que múltiples conexiones 
    /// compartan la misma BD en memoria, SIEMPRE que haya al menos UNA 
    /// conexión abierta manteniéndola viva.
    /// 
    /// ARQUITECTURA:
    /// 1. Test crea conexión "Keeper" que se mantiene abierta durante todo el test
    /// 2. Repository crea sus propias conexiones (con mismo connection string)
    /// 3. Al finalizar, Dispose() cierra la Keeper y la BD se destruye
    /// </summary>
    public class RepositorySharedTests : IDisposable
    {
        private readonly string _connectionString;
        private readonly SqliteConnection _connectionKeeper;
        private readonly ITestOutputHelper _output;

        // Repositorios bajo prueba
        private readonly IClienteRepository _clienteRepo;
        private readonly IProductoRepository _productoRepo;
        private readonly IFichaRepository _fichaRepo;

        public RepositorySharedTests(ITestOutputHelper output)
        {
            _output = output;

            // =================================================================
            // CONFIGURACIÓN CRÍTICA: Shared Cache
            // =================================================================
            // Sin Cache=Shared: Cada conexión nueva = BD vacía nueva
            // Con Cache=Shared: Todas las conexiones ven la misma BD
            _connectionString = "Data Source=:memory:;Cache=Shared";

            _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Iniciando test con Shared Cache");
            _output.WriteLine($"ConnectionString: {_connectionString}");

            // =================================================================
            // PASO 1: Crear "Keeper" - La conexión que mantiene viva la BD
            // =================================================================
            // Si cerramos esta conexión, la BD en memoria se destruye
            // aunque haya otras conexiones abiertas
            _connectionKeeper = new SqliteConnection(_connectionString);
            _connectionKeeper.Open();

            _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Keeper abierta. ID: {_connectionKeeper.GetHashCode()}");

            // =================================================================
            // PASO 2: Ejecutar Schema SQL en la conexión Keeper
            // =================================================================
            // Esto crea las tablas en la BD compartida
            EjecutarSchema();

            // =================================================================
            // PASO 3: Verificar que las tablas existen
            // =================================================================
            VerificarTablas();

            // =================================================================
            // PASO 4: Crear repositorios
            // =================================================================
            // Usan el MISMO connection string, pero crearán sus propias conexiones
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString
                })
                .Build();

            _clienteRepo = new ClienteRepository(config, NullLogger<ClienteRepository>.Instance);
            _productoRepo = new ProductoRepository(config, NullLogger<ProductoRepository>.Instance);
            _fichaRepo = new FichaRepository(config, NullLogger<FichaRepository>.Instance);

            _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Repositorios creados");
        }

        #region Setup Privado

        private void EjecutarSchema()
        {
            var schemaPath = BuscarSchemaSql();
            _output.WriteLine($"Schema encontrado: {schemaPath}");

            var schemaSql = File.ReadAllText(schemaPath);

            // Dividir en comandos individuales (SQLite no soporta múltiples statements bien)
            var comandos = schemaSql
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(cmd => cmd.Trim())
                .Where(cmd => !string.IsNullOrWhiteSpace(cmd) && !cmd.StartsWith("--"))
                .ToList();

            _output.WriteLine($"Comandos SQL a ejecutar: {comandos.Count}");

            using var transaction = _connectionKeeper.BeginTransaction();

            try
            {
                foreach (var comando in comandos)
                {
                    using var cmd = new SqliteCommand(comando + ";", _connectionKeeper, transaction);
                    cmd.ExecuteNonQuery();
                    _output.WriteLine($"  ✓ Ejecutado: {comando.Substring(0, Math.Min(50, comando.Length))}...");
                }

                transaction.Commit();
                _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Schema creado exitosamente");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ERROR en schema: {ex.Message}");
                throw;
            }
        }

        private void VerificarTablas()
        {
            const string sql = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
            using var cmd = new SqliteCommand(sql, _connectionKeeper);
            using var reader = cmd.ExecuteReader();

            _output.WriteLine("Tablas creadas:");
            while (reader.Read())
            {
                _output.WriteLine($"  - {reader.GetString(0)}");
            }
        }

        private string BuscarSchemaSql()
        {
            // Múltiples rutas posibles según cómo se ejecute el test
            var posiblesRutas = new[]
            {
                // Desde bin/Debug/net9.0/
                Path.Combine(AppContext.BaseDirectory, "Data", "Schema.sql"),
                // Desde carpeta de tests
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FichaCosto.Service", "Data", "Schema.sql"),
                // Ruta absoluta conocida
                @"D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service\Data\Schema.sql"
            };

            foreach (var ruta in posiblesRutas)
            {
                var rutaNormalizada = Path.GetFullPath(ruta);
                if (File.Exists(rutaNormalizada))
                    return rutaNormalizada;
            }

            throw new FileNotFoundException(
                "No se encontró Schema.sql. Buscado en:\n" +
                string.Join("\n", posiblesRutas.Select(p => Path.GetFullPath(p))));
        }

        #endregion

        #region Tests de ClienteRepository

        [Fact]
        public async Task Cliente_CRUD_Completo()
        {
            _output.WriteLine("\n=== TEST: Cliente_CRUD_Completo ===");

            // CREATE
            var cliente = new Cliente
            {
                NombreEmpresa = "Empresa Test S.A.",
                CUIT = "30111222333",
                Direccion = "Calle Falsa 123",
                ContactoTelefono = "555-0100",
                ContactoEmail = "test@empresa.com",
                Activo = true,
                FechaAlta = DateTime.Now
            };

            var id = await _clienteRepo.CreateAsync(cliente);
            _output.WriteLine($"CREATE: Cliente creado con ID {id}");
            Assert.True(id > 0);

            // READ
            var clienteLeido = await _clienteRepo.GetByIdAsync(id);
            _output.WriteLine($"READ: Cliente leído - {clienteLeido?.NombreEmpresa}");
            Assert.NotNull(clienteLeido);
            Assert.Equal(cliente.NombreEmpresa, clienteLeido.NombreEmpresa);
            Assert.Equal(cliente.CUIT, clienteLeido.CUIT);

            // UPDATE
            clienteLeido.NombreEmpresa = "Empresa Test Modificada S.A.";
            var actualizado = await _clienteRepo.UpdateAsync(clienteLeido);
            _output.WriteLine($"UPDATE: Resultado = {actualizado}");
            Assert.True(actualizado);

            var clienteActualizado = await _clienteRepo.GetByIdAsync(id);
            Assert.Equal("Empresa Test Modificada S.A.", clienteActualizado?.NombreEmpresa);

            // EXISTS
            var existe = await _clienteRepo.ExistsByCuitAsync(cliente.CUIT);
            _output.WriteLine($"EXISTS: CUIT {cliente.CUIT} existe = {existe}");
            Assert.True(existe);

            // LIST ALL
            var todos = await _clienteRepo.GetAllAsync();
            _output.WriteLine($"LIST: Total clientes = {todos.Count()}");

            // DELETE
            var eliminado = await _clienteRepo.DeleteAsync(id);
            _output.WriteLine($"DELETE: Resultado = {eliminado}");
            Assert.True(eliminado);

            var clienteEliminado = await _clienteRepo.GetByIdAsync(id);
            Assert.Null(clienteEliminado);
        }

        [Fact]
        public async Task Cliente_ExistsByCuit_DistingueEntreExistenteYNuevo()
        {
            _output.WriteLine("\n=== TEST: ExistsByCuit ===");

            // Arrange
            var cliente = new Cliente
            {
                NombreEmpresa = "CUIT Test",
                CUIT = "30999888777",
                Activo = true,
                FechaAlta = DateTime.Now
            };
            await _clienteRepo.CreateAsync(cliente);

            // Act & Assert
            Assert.True(await _clienteRepo.ExistsByCuitAsync("30999888777"));
            Assert.False(await _clienteRepo.ExistsByCuitAsync("30111111111"));
        }

        #endregion

        #region Tests de ProductoRepository

        [Fact]
        public async Task Producto_ConClienteYDetalles()
        {
            _output.WriteLine("\n=== TEST: Producto_ConClienteYDetalles ===");

            // Crear cliente primero
            var cliente = new Cliente
            {
                NombreEmpresa = "Cliente Producto",
                CUIT = "30777666555",
                Activo = true,
                FechaAlta = DateTime.Now
            };
            var clienteId = await _clienteRepo.CreateAsync(cliente);
            _output.WriteLine($"Cliente creado: {clienteId}");

            // Crear producto
            var producto = new Producto
            {
                ClienteId = clienteId,
                Codigo = "PROD-TEST-001",
                Nombre = "Producto de Prueba",
                Descripcion = "Descripción del producto",
                UnidadMedida = UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            var productoId = await _productoRepo.CreateAsync(producto);
            _output.WriteLine($"Producto creado: {productoId}");

            // Leer con detalles (debería tener listas vacías, no null)
            var productoLeido = await _productoRepo.GetByIdWithDetailsAsync(productoId);
            _output.WriteLine($"Producto leído: {productoLeido?.Nombre}");

            Assert.NotNull(productoLeido);
            Assert.NotNull(productoLeido.MateriasPrimas);
            Assert.NotNull(productoLeido.ManoObras);
            _output.WriteLine($"MateriasPrimas: {productoLeido.MateriasPrimas.Count} items");
            _output.WriteLine($"ManoObras: {productoLeido.ManoObras.Count} items");

            // Listar por cliente
            var productosDelCliente = await _productoRepo.GetByClienteIdAsync(clienteId);
            Assert.Single(productosDelCliente);
        }

        [Fact]
        public async Task Producto_ExistsByCodigo_ValidaUnicidad()
        {
            _output.WriteLine("\n=== TEST: ExistsByCodigo ===");

            // Crear cliente
            var clienteId = await _clienteRepo.CreateAsync(new Cliente
            {
                NombreEmpresa = "Cliente Codigo",
                CUIT = "30555444333",
                Activo = true,
                FechaAlta = DateTime.Now
            });

            // Crear producto
            await _productoRepo.CreateAsync(new Producto
            {
                ClienteId = clienteId,
                Codigo = "UNICO-001",
                Nombre = "Producto Unico",
                UnidadMedida = UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.Now
            });

            // Verificar existencia
            Assert.True(await _productoRepo.ExistsByCodigoAsync("UNICO-001"));
            Assert.False(await _productoRepo.ExistsByCodigoAsync("NO-EXISTE"));
        }

        #endregion

        #region Tests de FichaRepository

        [Fact]
        public async Task Ficha_HistorialCompleto()
        {
            _output.WriteLine("\n=== TEST: Ficha_HistorialCompleto ===");

            // Setup: Crear cliente y producto
            var clienteId = await _clienteRepo.CreateAsync(new Cliente
            {
                NombreEmpresa = "Cliente Ficha",
                CUIT = "30222111444",
                Activo = true,
                FechaAlta = DateTime.Now
            });

            var productoId = await _productoRepo.CreateAsync(new Producto
            {
                ClienteId = clienteId,
                Codigo = "PROD-HIST",
                Nombre = "Producto Historial",
                UnidadMedida = UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.Now
            });

            // Crear 5 fichas de costo
            for (int i = 1; i <= 5; i++)
            {
                var ficha = new FichaCostoEntity
                {
                    ProductoId = productoId,
                    FechaCalculo = DateTime.Now.AddDays(-i), // Días anteriores
                    CostoMateriasPrimas = 100m * i,
                    CostoManoObra = 50m * i,
                    //ToDo: CostosIndirectos = 25m,
                    CostosDirectosTotales = 25m,
                    //ToDo: GastosGenerales = 10m,
                    //ToDo:CostoTotal = (100m * i) + (50m * i) + 35m,
                    MargenUtilidad = 30m,
                    //ToDo: PrecioVentaSugerido = ((100m * i) + (50m * i) + 35m) * 1.3m,
                    EstadoValidacion = EstadoValidacion.Valido,
                    //ToDo:CalculadoPor = "TestUser"
                };

                var id = await _fichaRepo.CreateAsync(ficha);
                _output.WriteLine($"Ficha {i} creada: ID {id}");
            }

            // Obtener historial
            var historial = await _fichaRepo.GetHistorialByProductoIdAsync(productoId, 10);
            _output.WriteLine($"Historial recuperado: {historial.Count()} fichas");
            Assert.Equal(5, historial.Count());

            // Verificar orden (más reciente primero)
            var fechas = historial.Select(f => f.FechaCalculo).ToList();
            Assert.True(fechas[0] > fechas[1]); // Primera más reciente que segunda

            // Obtener última ficha
            var ultima = await _fichaRepo.GetUltimaFichaByProductoIdAsync(productoId);
            //ToDo: _output.WriteLine($"Última ficha: CostoTotal = {ultima?.CostoTotal}");
            Assert.NotNull(ultima);
            Assert.Equal(5, historial.First().Id); // La última creada tiene ID 5 (o el mayor)
        }

        #endregion

        #region Tests de Integración

        [Fact]
        public async Task EscenarioCompleto_CrearFichaDeCosto()
        {
            _output.WriteLine("\n=== TEST: EscenarioCompleto ===");

            // 1. Crear cliente
            var cliente = new Cliente
            {
                NombreEmpresa = "PyME Real S.A.",
                CUIT = "30123456789",
                Activo = true,
                FechaAlta = DateTime.Now
            };
            var clienteId = await _clienteRepo.CreateAsync(cliente);
            _output.WriteLine($"1. Cliente: {clienteId}");

            // 2. Crear producto
            var producto = new Producto
            {
                ClienteId = clienteId,
                Codigo = "PROD-REAL-001",
                Nombre = "Producto Terminado",
                UnidadMedida = UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.Now
            };
            var productoId = await _productoRepo.CreateAsync(producto);
            _output.WriteLine($"2. Producto: {productoId}");

            // 3. Simular cálculo de ficha
            var materiasPrimas = 150.50m;
            var manoObra = 75.25m;
            var indirectos = 25.00m;
            var generales = 10.00m;
            var costoTotal = materiasPrimas + manoObra + indirectos + generales;
            var margen = 30m;
            var precioVenta = costoTotal * (1 + margen / 100);

            var ficha = new FichaCostoEntity
            {
                ProductoId = productoId,
                FechaCalculo = DateTime.Now,
                CostoMateriasPrimas = materiasPrimas,
                CostoManoObra = manoObra,
                CostosDirectosTotales = indirectos,
                //ToDo: GastosGenerales = generales,
                //ToDo: CostoTotal = costoTotal,
                MargenUtilidad = margen,
                //ToDo: PrecioVentaSugerido = precioVenta,
                EstadoValidacion = margen <= 30 ? EstadoValidacion.Valido : EstadoValidacion.Excedido,
                //ToDo: Observaciones = "Ficha generada en test",
                //ToDo: CalculadoPor = "SistemaTest"
            };

            var fichaId = await _fichaRepo.CreateAsync(ficha);
            _output.WriteLine($"3. Ficha creada: {fichaId}");
            _output.WriteLine($"   Costo: ${costoTotal}, Precio: ${precioVenta}, Margen: {margen}%");

            // 4. Verificar integridad
            var clienteVerif = await _clienteRepo.GetByIdAsync(clienteId);
            var productoVerif = await _productoRepo.GetByIdAsync(productoId);
            var fichaVerif = await _fichaRepo.GetByIdAsync(fichaId);

            Assert.NotNull(clienteVerif);
            Assert.NotNull(productoVerif);
            Assert.NotNull(fichaVerif);
            Assert.Equal(clienteId, productoVerif.ClienteId);
            Assert.Equal(productoId, fichaVerif.ProductoId);

            _output.WriteLine("4. ✓ Todos los datos verificados correctamente");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _output.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] Disposing test...");

            // =================================================================
            // PASO CRÍTICO: Cerrar la conexión Keeper
            // =================================================================
            // Al cerrar esta conexión, la BD en memoria se destruye automáticamente
            // Esto garantiza limpieza completa entre tests

            if (_connectionKeeper != null)
            {
                if (_connectionKeeper.State == ConnectionState.Open)
                {
                    _connectionKeeper.Close();
                    _output.WriteLine("Keeper cerrada");
                }
                _connectionKeeper.Dispose();
                _output.WriteLine("Keeper disposed");
            }

            // Forzar GC para liberar recursos SQLite
            GC.Collect();
            GC.WaitForPendingFinalizers();

            _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Test completamente limpiado");
        }

        #endregion
    }

    // ========================================================================
    // ANEXO: Explicación Técnica del Problema y Solución
    // ========================================================================

    /*
    PROBLEMA ORIGINAL (sin Shared Cache):
    -------------------------------------
    
    Test:                    Repository:
    ------                   -----------
    1. Abre conexión A  ---> 2. Abre conexión B (nueva)
    3. Crea tablas            3. BD vacía (¡nueva!)
    4. Cierra A               4. "No such table: Clientes"
    
    Cada "new SqliteConnection(':memory:')" = BD nueva e independiente
    
    
    SOLUCIÓN CON SHARED CACHE:
    --------------------------
    
    Connection String: "Data Source=:memory:;Cache=Shared"
    
    Test (Keeper):           Repository:
    -------------            -----------
    1. Abre conexión A  ---> 2. Abre conexión B
    3. Crea tablas            3. ¡Misma BD! (compartida)
    4. Mantiene A ABIERTA     4. Ve las tablas creadas
       [durante todo test]
    5. Dispose: Cierra A      5. BD destruida automáticamente
    
    
    PUNTOS CLAVE:
    -------------
    1. Cache=Shared habilita el modo de caché compartido de SQLite
    2. La primera conexión crea la BD en memoria
    3. Conexiones subsiguientes con mismo string = misma BD
    4. La BD se mantiene viva mientras haya AL MENOS UNA conexión abierta
    5. Cuando la última conexión se cierra, la BD se destruye
    
    
    ALTERNATIVAS CONSIDERADAS:
    --------------------------
    
    A) SQLite en archivo temporal:
       - Pros: Más simple, no requiere Cache=Shared
       - Cons: I/O de disco más lento, requiere limpieza de archivos
    
    B) Inyectar conexión externa a repositories:
       - Pros: Control total sobre la conexión
       - Cons: Modifica la API de producción, código más complejo
    
    C) Connection Pooling (no funciona con :memory:):
       - SQLite en memoria no soporta pooling estándar
    
    
    POR QUÉ ELEGIMOS SHARED CACHE:
    ------------------------------
    - No requiere cambios en el código de producción (Repositories)
    - Limpieza automática al cerrar la conexión Keeper
    - Velocidad de ejecución (todo en RAM)
    - Aislamiento entre tests (cada test = nueva instancia de clase)
    */
}

