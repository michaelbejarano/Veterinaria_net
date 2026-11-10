using MediatR;
using vet.Domain.Ports;

namespace vet.Application.UseCases.User.Commands;

public class DeleteUserCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class DeleteUserCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteUserCommand, bool>
{
    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // Buscar usuario por ID
        var user = await unitOfWork.Users
            .GetByIdAsync(request.Id);

        // Si no existe, retornar false
        if (user is null)
            return false;

        // Desactivación lógica
        user.IsActive = false;

        // Guardar cambios en PostgreSQL
        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}