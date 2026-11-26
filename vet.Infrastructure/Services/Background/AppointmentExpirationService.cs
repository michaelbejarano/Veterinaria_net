using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using vet.Application.UseCases.Appointment.Commands;
using vet.Application.UseCases.AppointmentReschedule.Commands;

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

        try
        {
            // Primera ejecución al iniciar la aplicación

            await ProcessExpirationsAsync(stoppingToken);

            // Ejecutar cada minuto

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProcessExpirationsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Cierre normal de la aplicación
        }
    }

    private async Task ProcessExpirationsAsync(
        CancellationToken cancellationToken)
    {
        await ExpireAppointmentsAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        await ExpireReschedulesAsync(cancellationToken);
    }

    // Vencimiento de citas pendientes de adelanto

    private async Task ExpireAppointmentsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var mediator = scope.ServiceProvider
                .GetRequiredService<IMediator>();

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
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al procesar el vencimiento de citas.");
        }
    }

    // Vencimiento de reprogramaciones pendientes de pago

    private async Task ExpireReschedulesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var mediator = scope.ServiceProvider
                .GetRequiredService<IMediator>();

            var expiredCount = await mediator.Send(
                new ExpireAppointmentReschedulesCommand(),
                cancellationToken);

            if (expiredCount > 0)
            {
                _logger.LogInformation(
                    "VetCare: {Count} reprogramaciones expiradas.",
                    expiredCount);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al procesar el vencimiento de reprogramaciones.");
        }
    }
}