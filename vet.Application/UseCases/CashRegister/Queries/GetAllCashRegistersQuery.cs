using MediatR;
using vet.Domain.Ports;

using RegisterEntity = vet.Domain.Entities.CashRegister;
using SessionEntity = vet.Domain.Entities.CashSession;

namespace vet.Application.UseCases.CashRegister.Queries;

public class GetAllCashRegistersQuery
    : IRequest<List<CashRegisterDto>>
{
    public long? CompanyId { get; set; }
    public bool? IsActive { get; set; }
}

public class CashRegisterDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long? OpenSessionId { get; set; }
    public bool HasOpenSession => OpenSessionId.HasValue;
}

public class GetAllCashRegistersQueryHandler
    : IRequestHandler<GetAllCashRegistersQuery, List<CashRegisterDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllCashRegistersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CashRegisterDto>> Handle(
        GetAllCashRegistersQuery request,
        CancellationToken cancellationToken)
    {
        var registers = await _unitOfWork
            .Repository<RegisterEntity>()
            .FindAsync(x =>
                (!request.CompanyId.HasValue ||
                 x.CompanyId == request.CompanyId.Value) &&
                (!request.IsActive.HasValue ||
                 x.IsActive == request.IsActive.Value));

        var registerIds = registers
            .Select(x => x.Id)
            .ToList();

        var sessions = await _unitOfWork
            .Repository<SessionEntity>()
            .FindAsync(x =>
                registerIds.Contains(x.CashRegisterId) &&
                x.Status == "OPEN");

        var sessionDictionary = sessions
            .ToDictionary(x => x.CashRegisterId, x => x.Id);

        return registers
            .OrderBy(x => x.Name)
            .Select(x => new CashRegisterDto
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                Name = x.Name,
                IsActive = x.IsActive,
                OpenSessionId = sessionDictionary.TryGetValue(
                    x.Id, out var sessionId)
                    ? sessionId
                    : null
            })
            .ToList();
    }
}