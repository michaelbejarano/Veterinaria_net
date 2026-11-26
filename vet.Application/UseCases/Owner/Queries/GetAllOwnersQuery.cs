using MediatR;
using vet.Domain.Ports;

using OwnerEntity = vet.Domain.Entities.Owner;

namespace vet.Application.UseCases.Owner.Queries;

public class OwnerDto
{
    public long Id { get; set; }

    public string FirstNames { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetAllOwnersQuery : IRequest<List<OwnerDto>>
{
    public string? Search { get; set; }
}

public class GetAllOwnersQueryHandler
    : IRequestHandler<GetAllOwnersQuery, List<OwnerDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllOwnersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<OwnerDto>> Handle(
        GetAllOwnersQuery request,
        CancellationToken cancellationToken)
    {
        var repository = _unitOfWork
            .Repository<OwnerEntity>();

        var search = request.Search?
            .Trim()
            .ToLowerInvariant();

        var owners = string.IsNullOrWhiteSpace(search)
            ? await repository.GetAllAsync()
            : await repository.FindAsync(o =>
                o.FirstNames.ToLower().Contains(search) ||
                (o.DocumentNumber != null &&
                 o.DocumentNumber.ToLower().Contains(search)) ||
                (o.Phone != null &&
                 o.Phone.Contains(search)));

        return owners
            .OrderBy(o => o.FirstNames)
            .ThenBy(o => o.Id)
            .Select(o => new OwnerDto
            {
                Id = o.Id,
                FirstNames = o.FirstNames,
                DocumentType = o.DocumentType,
                DocumentNumber = o.DocumentNumber,
                Phone = o.Phone,
                Email = o.Email,
                Address = o.Address,
                CreatedAt = o.CreatedAt
            })
            .ToList();
    }
}