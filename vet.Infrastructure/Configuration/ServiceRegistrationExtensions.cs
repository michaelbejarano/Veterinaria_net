using MediatR;
using Microsoft.Extensions.DependencyInjection;
using vet.Application.UseCases.User.Commands;

namespace vet.Infrastructure.Configuration;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Registrar los Handlers CQRS de Application.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<
                CreateUserCommandHandler>());

        return services;
    }
}