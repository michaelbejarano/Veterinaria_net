
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Application.Services;

namespace vet.Infrastructure.Persistence.Seeders;

public static class SuperAdminSeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<VetCareContext>();

        var unitOfWork = scope.ServiceProvider
            .GetRequiredService<IUnitOfWork>();

        var passwordService = scope.ServiceProvider
            .GetRequiredService<IPasswordHashService>();

        var configuration = scope.ServiceProvider
            .GetRequiredService<IConfiguration>();

        // Obtener credenciales desde variables de entorno
        var email = configuration["Bootstrap:SuperAdminEmail"];
        var password = configuration["Bootstrap:SuperAdminPassword"];

        // No ejecutar si no se configuraron credenciales
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        email = email.Trim().ToLowerInvariant();

        // Validar contraseña
        if (password.Length < 12)
        {
            throw new InvalidOperationException(
                "La contraseña inicial debe tener al menos 12 caracteres.");
        }

        // Buscar rol SUPER_ADMIN
        var role = await context.Roles
            .FirstOrDefaultAsync(r => r.Name == "SUPER_ADMIN");

        if (role == null)
        {
            throw new InvalidOperationException(
                "El rol SUPER_ADMIN no existe en la base de datos.");
        }

        // Evitar crear otro SUPER_ADMIN mediante el seeder
        var existingSuperAdmin = await context.Users
            .AnyAsync(u => u.RoleId == role.Id);

        if (existingSuperAdmin)
        {
            return;
        }

        // Comprobar que el correo no esté registrado
        var existingEmail = await unitOfWork.Users
            .ExistsByEmailAsync(email);

        if (existingEmail)
        {
            throw new InvalidOperationException(
                "El correo inicial ya está registrado.");
        }

        // Crear usuario
        var user = new User
        {
            RoleId = role.Id,
            Email = email,
            IsActive = true
        };

        // Generar hash seguro
        user.PasswordHash =
            passwordService.HashPassword(password);

        // Guardar usuario mediante UnitOfWork
        await unitOfWork.Users.AddAsync(user);
        await unitOfWork.SaveChangesAsync();
    }
}
