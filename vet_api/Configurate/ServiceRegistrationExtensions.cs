using Microsoft.Extensions.DependencyInjection;
using vet.Application.UseCases.User.Commands;
using vet.Application.Validators.User;

namespace vet.Application.Configuiration;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Registrar automáticamente los Handlers CQRS
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<
                CreateUserCommandHandler>());

        // Registrar validadores de usuarios
        services.AddScoped<UserEmailValidator>();
        services.AddScoped<UserRoleValidator>();
        

        return services;
    }
}