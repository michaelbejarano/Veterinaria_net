using MediatR;
using vet.Domain.Ports;

using CommissionRuleEntity =
    vet.Domain.Entities.CommissionRule;

using CommissionEntity =
    vet.Domain.Entities.Commission;

namespace vet.Application.UseCases.CommissionRule.Commands;

public class DeleteCommissionRuleCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class DeleteCommissionRuleCommandHandler
    : IRequestHandler<DeleteCommissionRuleCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommissionRuleCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeleteCommissionRuleCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar regla

        var repository = _unitOfWork
            .Repository<CommissionRuleEntity>();

        var rule = await repository
            .GetByIdAsync(request.Id);

        if (rule is null)
            throw new KeyNotFoundException(
                "La regla de comisión no existe.");

        // 2. Verificar si tiene comisiones asociadas

        var commissions = await _unitOfWork
            .Repository<CommissionEntity>()
            .FindAsync(x => x.RuleId == rule.Id);

        if (commissions.Any())
            throw new ArgumentException(
                "No se puede eliminar una regla que ya generó " +
                "comisiones. Finalice su vigencia.");

        // 3. Eliminar regla

        repository.Delete(rule);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}