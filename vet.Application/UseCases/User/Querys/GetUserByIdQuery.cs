using MediatR;
using vet.Domain.DTOs;
using vet.Domain.Ports;

namespace vet.Application.UseCases.User.Querys;

public class GetUserByIdQuery : IRequest<UserDto?>
{
    public long Id { get; set; }
}

public class GetUserByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    public async Task<UserDto?> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await unitOfWork
            .Repository<vet.Domain.Entities.User>()
            .GetByIdAsync(request.Id);

        if (user == null)
            return null;

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            RoleId = user.RoleId,
            IsActive = user.IsActive
        };
    }
}