
using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.IServices;
using vet.Application.Services;
using vet.Domain.Ports;
using vet.Domain.Services;
using RoleEntity = vet.Domain.Entities.Role;
using UserEntity = vet.Domain.Entities.User;

namespace vet.Application.UseCases.User.Commands;

public class CreateUserCommand : IRequest<long>
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(12)]
    public string Password { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long RoleId { get; set; }
}

public class CreateUserCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordHashService passwordHashService,
    UserEmailValidator emailValidator,
    UserPasswordValidator passwordValidator,
    UserAuthorizationService authorizationService
) : IRequestHandler<CreateUserCommand, long>
{
    public async Task<long> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Comprobar el administrador autenticado.
        var currentRole =
            await authorizationService.GetCurrentRoleAsync();

        // Verificar que el rol solicitado exista.
        var targetRole = await unitOfWork
            .Repository<RoleEntity>()
            .GetByIdAsync(request.RoleId);

        if (targetRole is null)
            throw new ArgumentException(
                "El rol seleccionado no existe.");

        // Aplicar las reglas del dominio.
        UserRoleRules.ValidateRoleAssignment(
            currentRole,
            targetRole.Name);

        // Validar correo y comprobar duplicados.
        var email = emailValidator.Validate(request.Email);

        if (await unitOfWork.Users.ExistsByEmailAsync(email))
            throw new InvalidOperationException(
                "El correo electrónico ya está registrado.");

        // Validar contraseña.
        passwordValidator.Validate(request.Password);

        // Crear usuario.
        var user = new UserEntity
        {
            Email = email,
            RoleId = request.RoleId,
            PasswordHash = passwordHashService.HashPassword(
                request.Password),
            IsActive = true
        };

        await unitOfWork.Users.AddAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
