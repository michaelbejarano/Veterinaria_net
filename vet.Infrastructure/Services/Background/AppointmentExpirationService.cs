using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using vet.Application.UseCases.Appointment.Commands;

namespace vet.Infrastructure.Services.Background;

public class AppointmentExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentExpirationService> _logger;

    public AppointmentExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<AppointmentExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(1));

        // Primera ejecución al iniciar la aplicación
        await ExpireAppointmentsAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExpireAppointmentsAsync(stoppingToken);
        }
    }

    private async Task ExpireAppointmentsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            // Crear un scope para resolver MediatR y UnitOfWork
            using var scope = _scopeFactory.CreateScope();

            var mediator = scope.ServiceProvider
                .GetRequiredService<IMediator>();

            // Ejecutar el caso de uso de Application
            var expiredCount = await mediator.Send(
                new ExpirePendingAppointmentsCommand(),
                cancellationToken);

            if (expiredCount > 0)
            {
                _logger.LogInformation(
                    "VetCare: {Count} citas expiradas.",
                    expiredCount);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Cierre normal de la aplicación
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al procesar el vencimiento de citas.");
        }
    }
}