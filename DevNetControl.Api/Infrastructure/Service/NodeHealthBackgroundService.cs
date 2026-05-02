using DevNetControl.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevNetControl.Api.Infrastructure.Services;

/// <summary>
/// Servicio de background para monitoreo automático de disponibilidad de nodos.
/// </summary>
public class NodeHealthBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NodeHealthBackgroundService> _logger;

    public NodeHealthBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NodeHealthBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de monitoreo de nodos iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var healthService = scope.ServiceProvider.GetRequiredService<NodeHealthService>();
                
                await healthService.CheckAllNodesAsync();
                _logger.LogInformation("Verificación de salud de nodos completada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el monitoreo de nodos.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Cada 5 minutos
        }

        _logger.LogInformation("Servicio de monitoreo de nodos detenido.");
    }
}
