using FichaCosto.Service.Data;

namespace FichaCosto.Data
{
    /// <summary>
    /// Servicio hosted que inicializa la base de datos al arrancar la aplicación.
    /// </summary>
    public class DatabaseInitializationService : IHostedService
    {
        private readonly DatabaseInitializer _initializer;
        private readonly ILogger<DatabaseInitializationService> _logger;

        public DatabaseInitializationService(
            DatabaseInitializer initializer,
            ILogger<DatabaseInitializationService> logger)
        {
            _initializer = initializer;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Inicializando base de datos...");

            try
            {
                await _initializer.InitializeAsync();
                _logger.LogInformation("Base de datos inicializada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al inicializar la base de datos");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // No requiere acción al detener
            return Task.CompletedTask;
        }
    }
}