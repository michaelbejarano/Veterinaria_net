
using Microsoft.Extensions.DependencyInjection;
using vet.Application.UseCases.User.Commands;
using vet.Application.Validators.Employee;
using vet.Application.Validators.EmployeeSchedule;
using vet.Application.Validators.User;
using vet.Application.Services;

namespace vet_api.Configurate;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Registrar los Handlers de MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<
                CreateUserCommandHandler>());

        // Nuevos servicios de dominio
        services.AddScoped<vet.Domain.Services.UserEmailValidator>();
        services.AddScoped<vet.Domain.Services.UserPasswordValidator>();

        // Validadores antiguos, temporalmente necesarios
        services.AddScoped<UserEmailValidator>();
        services.AddScoped<UserRoleValidator>();
        services.AddScoped<UserPasswordValidator>();
        services.AddScoped<UserPermissionValidator>();

        // Validadores de empleados
        services.AddScoped<EmployeeValidator>();
        services.AddScoped<EmployeePermissionValidator>();

        // Validadores de horarios
        services.AddScoped<EmployeeScheduleValidator>();
    
        services.AddScoped<UserAuthorizationService>();
        return services;
    }
}
