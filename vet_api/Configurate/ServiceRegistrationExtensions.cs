using MediatR;
using Microsoft.Extensions.DependencyInjection;
using vet.Application.UseCases.User.Querys;

namespace vet.Application.Configuration;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<
                GetAllUserQueryHandler>());

        return services;
    }
}