using Microsoft.Extensions.DependencyInjection;
using vet.Application.UseCases.User.Commands;
using vet.Application.Validators.User;

namespace vet_api.Configurate;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Registrar Handlers de MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<
                CreateUserCommandHandler>());

        // Validadores de usuarios
        services.AddScoped<UserEmailValidator>();
        services.AddScoped<UserRoleValidator>();
        services.AddScoped<UserPasswordValidator>();

        // Validador de permisos según el rol autenticado
        services.AddScoped<UserPermissionValidator>();

        return services;
    }
}