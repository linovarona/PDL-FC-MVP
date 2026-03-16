using Dapper;
using FichaCosto.Repositories.Implementations;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Models.Entities;
using FichaCosto.Service.Models.Enums;
using FichaCostoEntity = FichaCosto.Service.Models.Entities.FichaCosto;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using Xunit;
using Xunit.Abstractions;

namespace FichaCosto.Service.Tests
{
    /// <summary>
    /// Tests de integración con conexión SQLite compartida mediante IConnectionFactory.
    /// 
    /// ARQUITECTURA:
    /// - Una sola SqliteConnection abierta durante todo el test
    /// - TestConnectionFactory envuelve la conexión en NonDisposableConnection
    /// - Repositories reciben IConnectionFactory (no IConfiguration)
    /// - NonDisposableConnection ignora Dispose() para mantener BD viva
    /// </summary>
    public class RepositorySharedTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly SqliteConnection _sharedConnection;
        private readonly IClienteRepository _clienteRepo;
        private readonly IProductoRepository _productoRepo;
        private readonly IFichaRepository _fichaRepo;

        public RepositorySharedTests(ITestOutputHelper output)
        {
            _output = output;

            _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] === INICIO TEST ===");
            _output.WriteLine($"Thread ID: {Environment.CurrentManagedThreadId}");

            // ============================================================
            // PASO 1: Crear y abrir conexión SQLite en memoria
            // ============================================================
            var connectionString = "Data Source=:memory:";

            _sharedConnection = new SqliteConnection(connectionString);
            _sharedConnection.Open();

            _output.WriteLine($"Conexión abierta. State: {_sharedConnection.State}, Hash: {_sharedConnection.GetHashCode()}");

            // ============================================================
            // PASO 2: Crear schema en la conexión compartida
            // ============================================================
            EjecutarSchema();

            // ============================================================
            // PASO 3: Verificar tablas existen
            // ============================================================
            VerificarTablas();

            // ============================================================
            // PASO 4: Crear IConnectionFactory con NonDisposableConnection
            // ============================================================
            IConnectionFactory factory = new TestConnectionFactory(_sharedConnection);
            _output.WriteLine($"Factory creada. Tipo: {factory.GetType().Name}");

            // ============================================================
            // PASO 5: Crear repositorios con IConnectionFactory (NO IConfiguration)
            // ============================================================
            _clienteRepo = new ClienteRepository(factory, NullLogger<ClienteRepository>.Instance);
            _productoRepo = new ProductoRepository(factory, NullLogger<ProductoRepository>.Instance);
            _fichaRepo = new FichaRepository(factory, NullLogger<FichaRepository>.Instance);

            _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Repositorios listos");
        }

        #region Setup Privado

        private void EjecutarSchema()
        {
            var schemaPath = BuscarSchemaSql();
            var schemaSql = File.ReadAllText(schemaPath);

            // Ejecutar todo el schema en la conexión compartida
            using var cmd = new SqliteCommand(schemaSql, _sharedConnection);
            cmd.ExecuteNonQuery();

            _output.WriteLine($"Schema ejecutado en conexión {_sharedConnection.GetHashCode()}");
        }

        private void VerificarTablas()
        {
            const string sql = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
            using var cmd = new SqliteCommand(sql, _sharedConnection);
            using var reader = cmd.ExecuteReader();

            var tablas = new List<string>();
            while (reader.Read())
            {
                tablas.Add(reader.GetString(0));
            }

            _output.WriteLine($"Tablas en BD: {string.Join(", ", tablas)}");

            if (!tablas.Contains("Clientes"))
            {
                throw new InvalidOperationException("ERROR CRÍTICO: Tabla Clientes no existe");
            }
        }

        private string BuscarSchemaSql()
        {
            var rutas = new[]
            {
                // Desde output del test
                Path.Combine(AppContext.BaseDirectory, "Data", "Schema.sql"),
                // Desde proyecto de tests
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FichaCosto.Service", "Data", "Schema.sql"),
                // Desde carpeta de tests
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "Schema.sql"),
                // Ruta absoluta conocida
                @"D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service\Data\Schema.sql"
            };

            foreach (var ruta in rutas.Select(Path.GetFullPath))
            {
                if (File.Exists(ruta))
                {
                    _output.WriteLine($"Schema encontrado: {ruta}");
                    return ruta;
                }
            }

            throw new FileNotFoundException("Schema.sql no encontrado en rutas de búsqueda");
        }

        #endregion

        #region Tests de ClienteRepository

        [Fact]
        public async Task Cliente_CRUD_Completo()
        {
            _output.WriteLine($"\n--- TEST: Cliente_CRUD_Completo ---");

            // CREATE
            var cliente = new Cliente
            {
                NombreEmpresa = "Empresa Test S.A.",
                CUIT = "30111222333",
                Direccion = "Calle Falsa 123",
                ContactoNombre = "Juan Pérez",
                ContactoTelefono = "555-0100",
                ContactoEmail = "test@empresa.com",
                Activo = true,
                FechaAlta = DateTime.UtcNow
            };

            _output.WriteLine("Creando cliente...");
            var id = await _clienteRepo.CreateAsync(cliente);
            _output.WriteLine($"✓ Creado ID: {id}");
            Assert.True(id > 0);

            // READ
            _output.WriteLine($"Leyendo cliente {id}...");
            var leido = await _clienteRepo.GetByIdAsync(id);
            Assert.NotNull(leido);
            Assert.Equal(cliente.NombreEmpresa, leido.NombreEmpresa);
            Assert.Equal(cliente.CUIT, leido.CUIT);
            _output.WriteLine($"✓ Leído: {leido.NombreEmpresa}");

            // UPDATE
            leido.NombreEmpresa = "Empresa Modificada S.A.";
            leido.ContactoEmail = "nuevo@empresa.com";
            _output.WriteLine("Actualizando cliente...");
            var actualizado = await _clienteRepo.UpdateAsync(leido);
            Assert.True(actualizado);

            var actualizadoVerif = await _clienteRepo.GetByIdAsync(id);
            Assert.Equal("Empresa Modificada S.A.", actualizadoVerif?.NombreEmpresa);
            _output.WriteLine("✓ Actualizado");

            // EXISTS
            var existe = await _clienteRepo.ExistsByCuitAsync(cliente.CUIT);
            Assert.True(existe);
            _output.WriteLine("✓ Exists verificado");

            // LIST ALL
            var todos = await _clienteRepo.GetAllAsync();
            Assert.Contains(todos, c => c.Id == id);
            _output.WriteLine($"✓ Listado: {todos.Count()} clientes");

            // DELETE
            _output.WriteLine("Eliminando cliente...");
            var eliminado = await _clienteRepo.DeleteAsync(id);
            Assert.True(eliminado);

            var verificarEliminado = await _clienteRepo.GetByIdAsync(id);
            Assert.Null(verificarEliminado);
            _output.WriteLine("✓ Eliminado y verificado");

            _output.WriteLine("--- TEST COMPLETADO ---");
        }

        [Fact]
        public async Task Cliente_ExistsByCuit_DistingueExistenteYNuevo()
        {
            _output.WriteLine($"\n--- TEST: ExistsByCuit ---");

            var cliente = new Cliente
            {
                NombreEmpresa = "CUIT Test",
                CUIT = "30999888777",
                Activo = true,
                FechaAlta = DateTime.UtcNow
            };
            await _clienteRepo.CreateAsync(cliente);

            Assert.True(await _clienteRepo.ExistsByCuitAsync("30999888777"));
            Assert.False(await _clienteRepo.ExistsByCuitAsync("30111111111"));

            _output.WriteLine("✓ Exists distingue correctamente");
        }

        #endregion

        #region Tests de ProductoRepository

        [Fact]
        public async Task Producto_ConClienteYDetalles()
        {
            _output.WriteLine($"\n--- TEST: Producto_ConClienteYDetalles ---");

            // Setup: Cliente
            var clienteId = await _clienteRepo.CreateAsync(new Cliente
            {
                NombreEmpresa = "Cliente Producto",
                CUIT = "30777666555",
                Activo = true,
                FechaAlta = DateTime.UtcNow
            });
            _output.WriteLine($"Cliente creado: {clienteId}");

            // Producto
            var producto = new Producto
            {
                ClienteId = clienteId,
                Codigo = "PROD-001",
                Nombre = "Producto Test",
                Descripcion = "Descripción de prueba",
                UnidadMedida = UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var productoId = await _productoRepo.CreateAsync(producto);
            _output.WriteLine($"Producto creado: {productoId}");

            // Leer con detalles
            var conDetalles = await _productoRepo.GetByIdWithDetailsAsync(productoId);

            Assert.NotNull(conDetalles);
            Assert.NotNull(conDetalles.MateriasPrimas);
            Assert.NotNull(conDetalles.ManoObras);
            Assert.Equal(clienteId, conDetalles.ClienteId);

            _output.WriteLine($"✓ Detalles: MP={conDetalles.MateriasPrimas.Count}, MO={conDetalles.ManoObras.Count}");

            // Listar por cliente
            var productosDelCliente = await _productoRepo.GetByClienteIdAsync(clienteId);
            Assert.Single(productosDelCliente);
            _output.WriteLine("✓ Listado por cliente correcto");
        }

        [Fact]
        public async Task Producto_ExistsByCodigo_ValidaUnicidad()
        {
            _output.WriteLine($"\n--- TEST: ExistsByCodigo ---");

            var clienteId = await _clienteRepo.CreateAsync(new Cliente
            {
                NombreEmpresa = "Cliente Codigo",
                CUIT = "30555444333",
                Activo = true,
                FechaAlta = DateTime.UtcNow
            });

            await _productoRepo.CreateAsync(new Producto
            {
                ClienteId = clienteId,
                Codigo = "UNICO-001",
                Nombre = "Producto Unico",
                UnidadMedida = UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });

            Assert.True(await _productoRepo.ExistsByCodigoAsync("UNICO-001"));
            Assert.False(await _productoRepo.ExistsByCodigoAsync("NO-EXISTE"));

            // Verificar excludeId
            var producto2 = await _productoRepo.GetByIdAsync(1);
            if (producto2 != null)
            {
                Assert.False(await _productoRepo.ExistsByCodigoAsync("UNICO-001", excludeId: producto2.Id));
            }

            _output.WriteLine("✓ ExistsByCodigo funciona correctamente");
        }

        #endregion

        #region Tests de FichaRepository (MVP - Sin campos no habilitados)

        [Fact]
        public async Task Ficha_CRUD_SinCamposNoHabilitados()
        {
            _output.WriteLine($"\n--- TEST: Ficha_CRUD_SinCamposNoHabilitados ---");

            // Setup
            var clienteId = await _clienteRepo.CreateAsync(new Cliente
            {
                NombreEmpresa = "Cliente Ficha",
                CUIT = "30222111444",
                Activo = true,
                FechaAlta = DateTime.UtcNow
            });

            var productoId = await _productoRepo.CreateAsync(new Producto
            {
                ClienteId = clienteId,
                Codigo = "PROD-FICHA",
                Nombre = "Producto Con Ficha",
                UnidadMedida = UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });
            _output.WriteLine($"Setup: Cliente {clienteId}, Producto {productoId}");

            // Ficha SIN CostosIndirectos y GastosGenerales (post-MVP)
            var ficha = new FichaCostoEntity
            {
                ProductoId = productoId,
                FechaCalculo = DateTime.UtcNow,
                CostoMateriasPrimas = 100.50m,
                CostoManoObra = 50.25m,
                // NO incluir: CostosIndirectos, GastosGenerales
                //ToDo:CostoTotal = 150.75m,
                MargenUtilidad = 30.0m,
                //ToDo: PrecioVentaSugerido = 195.98m,
                EstadoValidacion = EstadoValidacion.Valido,
                //ToDo: = "Ficha de prueba MVP",
                //ToDo:CalculadoPor = "TestUser"
            };

            var id = await _fichaRepo.CreateAsync(ficha);
            _output.WriteLine($"Ficha creada: ID {id}");
            Assert.True(id > 0);

            // READ
            var leida = await _fichaRepo.GetByIdAsync(id);
            Assert.NotNull(leida);
            //ToDo:Assert.Equal(ficha.CostoTotal, leida.CostoTotal);
            Assert.Equal(ficha.MargenUtilidad, leida.MargenUtilidad);
            //ToDo:_output.WriteLine($"✓ Leída: CostoTotal=${leida.CostoTotal}");

            // Historial
            var historial = await _fichaRepo.GetHistorialByProductoIdAsync(productoId, 10);
            Assert.Single(historial);
            _output.WriteLine($"✓ Historial: {historial.Count()} fichas");

            // Última ficha
            var ultima = await _fichaRepo.GetUltimaFichaByProductoIdAsync(productoId);
            Assert.NotNull(ultima);
            Assert.Equal(id, ultima.Id);
            _output.WriteLine($"✓ Última ficha recuperada correctamente");

            // DELETE
            var eliminada = await _fichaRepo.DeleteAsync(id);
            Assert.True(eliminada);
            Assert.Null(await _fichaRepo.GetByIdAsync(id));
            _output.WriteLine("✓ Eliminada correctamente");
        }

        [Fact]
        public async Task Ficha_HistorialMultiple()
        {
            _output.WriteLine($"\n--- TEST: Ficha_HistorialMultiple ---");

            // Setup
            var clienteId = await _clienteRepo.CreateAsync(new Cliente
            {
                NombreEmpresa = "Historial Test",
                CUIT = "30333333333",
                Activo = true,
                FechaAlta = DateTime.UtcNow
            });

            var productoId = await _productoRepo.CreateAsync(new Producto
            {
                ClienteId = clienteId,
                Codigo = "HIST-001",
                Nombre = "Producto Historial",
                UnidadMedida = UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });

            // Crear 5 fichas con fechas diferentes
            for (int i = 1; i <= 5; i++)
            {
                var ficha = new FichaCostoEntity
                {
                    ProductoId = productoId,
                    FechaCalculo = DateTime.UtcNow.AddDays(-i), // Más antiguas
                    CostoMateriasPrimas = 100m * i,
                    CostoManoObra = 50m * i,
                    //ToDo:CostoTotal = 150m * i,
                    MargenUtilidad = 25m + i,
                    //ToDo:PrecioVentaSugerido = (150m * i) * (1 + (25m + i) / 100),
                    EstadoValidacion = EstadoValidacion.Valido,
                    //ToDo:CalculadoPor = $"TestUser{i}"
                };
                await _fichaRepo.CreateAsync(ficha);
            }

            // Verificar historial ordenado (más reciente primero)
            var historial = await _fichaRepo.GetHistorialByProductoIdAsync(productoId, 10);
            Assert.Equal(5, historial.Count());

            var fechas = historial.Select(f => f.FechaCalculo).ToList();
            Assert.True(fechas[0] > fechas[1]); // Primera más reciente que segunda
            Assert.True(fechas[1] > fechas[2]); // Y así sucesivamente

            _output.WriteLine($"✓ {historial.Count()} fichas en orden cronológico inverso");

            // Verificar limit
            var limitado = await _fichaRepo.GetHistorialByProductoIdAsync(productoId, 3);
            Assert.Equal(3, limitado.Count());
            _output.WriteLine("✓ Limit funciona correctamente");
        }

        #endregion

        #region Tests de Integración

        [Fact]
        public async Task EscenarioCompleto_CrearFichaDeCosto()
        {
            _output.WriteLine($"\n--- TEST: EscenarioCompleto ---");

            // 1. Crear cliente
            var cliente = new Cliente
            {
                NombreEmpresa = "PyME Real S.A.",
                CUIT = "30123456789",
                Direccion = "Av. Siempre Viva 742",
                ContactoNombre = "Homero Simpson",
                ContactoTelefono = "555-0199",
                ContactoEmail = "homero@pyme.com",
                Activo = true,
                FechaAlta = DateTime.UtcNow
            };
            var clienteId = await _clienteRepo.CreateAsync(cliente);
            _output.WriteLine($"1. Cliente: {clienteId}");

            // 2. Crear producto
            var producto = new Producto
            {
                ClienteId = clienteId,
                Codigo = "DONA-001",
                Nombre = "Donas Rosadas",
                Descripcion = "Producto estrella",
                UnidadMedida = UnidadMedida.Unidad,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            var productoId = await _productoRepo.CreateAsync(producto);
            _output.WriteLine($"2. Producto: {productoId}");

            // 3. Calcular y guardar ficha (simulación de cálculo)
            var materiasPrimas = 25.50m; // Masa, glaseado
            var manoObra = 15.00m;       // Preparación
            var costoTotal = materiasPrimas + manoObra;
            var margen = 30m;
            var precioVenta = costoTotal * (1 + margen / 100);

            var ficha = new FichaCostoEntity
            {
                ProductoId = productoId,
                FechaCalculo = DateTime.UtcNow,
                CostoMateriasPrimas = materiasPrimas,
                CostoManoObra = manoObra,
                CostoTotal = costoTotal,
                MargenUtilidad = margen,
                PrecioVentaSugerido = precioVenta,
                EstadoValidacion = margen <= 30 ? EstadoValidacion.Valido : EstadoValidacion.Excedido,
                Observaciones = "Ficha generada en test de integración",
                CalculadoPor = "SistemaTest",
                //Yo:
                CostosDirectosTotales = costoTotal,
                PrecioVentaCalculado  = precioVenta
            };

            var fichaId = await _fichaRepo.CreateAsync(ficha);
            _output.WriteLine($"3. Ficha: {fichaId} (Costo: ${costoTotal}, Precio: ${precioVenta:F2})");

            // 4. Verificar integridad de datos
            var clienteVerif = await _clienteRepo.GetByIdAsync(clienteId);
            var productoVerif = await _productoRepo.GetByIdAsync(productoId);
            var fichaVerif = await _fichaRepo.GetByIdAsync(fichaId);

            Assert.NotNull(clienteVerif);
            Assert.NotNull(productoVerif);
            Assert.NotNull(fichaVerif);

            Assert.Equal(clienteId, productoVerif.ClienteId);
            Assert.Equal(productoId, fichaVerif.ProductoId);
            //ToDo:Assert.Equal(costoTotal, fichaVerif.CostoTotal);

            // 5. Verificar historial
            var historial = await _fichaRepo.GetHistorialByProductoIdAsync(productoId);
            Assert.Single(historial);

            _output.WriteLine("4. ✓ Integridad verificada");
            _output.WriteLine("--- ESCENARIO COMPLETADO ---");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _output.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] === LIMPIEZA ===");

            // Cerrar la conexión real destruye la BD en memoria
            if (_sharedConnection?.State == ConnectionState.Open)
            {
                _sharedConnection.Close();
                _output.WriteLine("Conexión cerrada. BD en memoria destruida.");
            }

            _sharedConnection?.Dispose();
            _output.WriteLine("=== FIN TEST ===");
        }

        #endregion
    }
}