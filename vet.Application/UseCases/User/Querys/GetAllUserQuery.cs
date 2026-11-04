using MediatR;
using vet.Domain.DTOs;
using vet.Domain.Ports;

namespace vet.Application.UseCases.User.Querys;

public class GetAllUserQuery : IRequest<IEnumerable<UserDto>>
{
}

public class GetAllUserQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllUserQuery, IEnumerable<UserDto>>
{
    public async Task<IEnumerable<UserDto>> Handle(
        GetAllUserQuery request,
        CancellationToken cancellationToken)
    {
        var users = await unitOfWork
            .Repository<vet.Domain.Entities.User>()
            .GetAllAsync();

        return users.Select(user => new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            RoleId = user.RoleId,
            IsActive = user.IsActive
            
        }).ToList();
    }
}