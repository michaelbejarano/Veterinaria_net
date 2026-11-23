using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using vet.Application.IServices;
using vet.Domain.Ports;
using vet.Infrastructure.Persistence;
using vet.Infrastructure.Persistence.Repositories;
using vet.Infrastructure.Services.Security;
using vet.Infrastructure.Services.Background;

namespace vet.Infrastructure.Configurate;

public static class InfrastructureServicesExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("VetCareConnection")
            ?? throw new InvalidOperationException(
                "No se encontró la conexión VetCareConnection.");

        // PostgreSQL - Entity Framework Core
        services.AddDbContext<VetCareContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositorio genérico
        services.AddScoped(
            typeof(IGenericRepository<>),
            typeof(GenericRepository<>));

        // Repositorios específicos
        services.AddScoped<IUserRepository, UserRepository>();

        // Seguridad y autenticación
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Usuario autenticado
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        //background deneus de 15 m
        services.AddHostedService<AppointmentExpirationService>();

        return services;
    }
}