using MediatR;
using vet.Domain.Ports;

using CommissionRuleEntity =
    vet.Domain.Entities.CommissionRule;

using EmployeeServiceEntity =
    vet.Domain.Entities.EmployeeService;

namespace vet.Application.UseCases.CommissionRule.Queries;

public class GetCommissionRulesByEmployeeServiceQuery
    : IRequest<List<CommissionRuleDto>>
{
    public long EmployeeServiceId { get; set; }
}

public class GetCommissionRulesByEmployeeServiceQueryHandler
    : IRequestHandler<
        GetCommissionRulesByEmployeeServiceQuery,
        List<CommissionRuleDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCommissionRulesByEmployeeServiceQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CommissionRuleDto>> Handle(
        GetCommissionRulesByEmployeeServiceQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar asignación

        var employeeService = await _unitOfWork
            .Repository<EmployeeServiceEntity>()
            .GetByIdAsync(request.EmployeeServiceId);

        if (employeeService is null)
            throw new KeyNotFoundException(
                "La asignación del servicio no existe.");

        // 2. Consultar reglas

        var rules = await _unitOfWork
            .Repository<CommissionRuleEntity>()
            .FindAsync(x =>
                x.EmployeeServiceId ==
                request.EmployeeServiceId);

        return rules
            .OrderByDescending(x => x.ValidFrom)
            .Select(x => new CommissionRuleDto
            {
                Id = x.Id,
                EmployeeServiceId = x.EmployeeServiceId,
                Type = x.Type,
                Value = x.Value,
                ValidFrom = x.ValidFrom,
                ValidTo = x.ValidTo
            })
            .ToList();
    }
}