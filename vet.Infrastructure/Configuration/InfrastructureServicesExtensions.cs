using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using vet.Application.Services;
using vet.Domain.Ports;
using vet.Infrastructure.Adapters.Security;
using vet.Infrastructure.Persistence;
using vet.Infrastructure.Persistence.Repositories;



namespace vet.Infrastructure.Configuration;

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
            options.UseNpgsql(connectionString)
        );

        // Repositorio genérico
        services.AddScoped(
            typeof(IGenericRepository<>),
            typeof(GenericRepository<>));

        // Repositorios específicos
        services.AddScoped<IUserRepository, UserRepository>();
        
        // Adaptadores de seguridad
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        
        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        //JWT
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}