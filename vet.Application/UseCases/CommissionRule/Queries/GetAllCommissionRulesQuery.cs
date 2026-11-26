using MediatR;
using vet.Domain.Ports;

using CommissionRuleEntity =
    vet.Domain.Entities.CommissionRule;

namespace vet.Application.UseCases.CommissionRule.Queries;

public class CommissionRuleDto
{
    public long Id { get; set; }

    public long EmployeeServiceId { get; set; }

    public string Type { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }
}

public class GetAllCommissionRulesQuery
    : IRequest<List<CommissionRuleDto>>
{
}

public class GetAllCommissionRulesQueryHandler
    : IRequestHandler<
        GetAllCommissionRulesQuery,
        List<CommissionRuleDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllCommissionRulesQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CommissionRuleDto>> Handle(
        GetAllCommissionRulesQuery request,
        CancellationToken cancellationToken)
    {
        var rules = await _unitOfWork
            .Repository<CommissionRuleEntity>()
            .GetAllAsync();

        return rules
            .OrderBy(x => x.EmployeeServiceId)
            .ThenByDescending(x => x.ValidFrom)
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