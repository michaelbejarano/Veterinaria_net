using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.IServices;
using vet.Application.Validators.User;
using vet.Domain.Ports;
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
    UserRoleValidator roleValidator,
    UserPasswordValidator passwordValidator,
    UserPermissionValidator permissionValidator
) : IRequestHandler<CreateUserCommand, long>
{
    public async Task<long> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Comprobar autorización antes de crear
        await permissionValidator.ValidateCreateAsync(request.RoleId);

        // Validaciones existentes
        var email = await emailValidator.ValidateAsync(request.Email);

        await roleValidator.ValidateAsync(request.RoleId);

        passwordValidator.Validate(request.Password);

        // Crear usuario
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