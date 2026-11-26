using MediatR;
using vet.Domain.Ports;

using CommissionRuleEntity =
    vet.Domain.Entities.CommissionRule;

namespace vet.Application.UseCases.CommissionRule.Queries;

public class GetCommissionRuleByIdQuery
    : IRequest<CommissionRuleDto>
{
    public long Id { get; set; }
}

public class GetCommissionRuleByIdQueryHandler
    : IRequestHandler<
        GetCommissionRuleByIdQuery,
        CommissionRuleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCommissionRuleByIdQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CommissionRuleDto> Handle(
        GetCommissionRuleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _unitOfWork
            .Repository<CommissionRuleEntity>()
            .GetByIdAsync(request.Id);

        if (rule is null)
            throw new KeyNotFoundException(
                "La regla de comisión no existe.");

        return new CommissionRuleDto
        {
            Id = rule.Id,
            EmployeeServiceId = rule.EmployeeServiceId,
            Type = rule.Type,
            Value = rule.Value,
            ValidFrom = rule.ValidFrom,
            ValidTo = rule.ValidTo
        };
    }
}