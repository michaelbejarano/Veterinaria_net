using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.Commission;

using CommissionRuleEntity =
    vet.Domain.Entities.CommissionRule;

using CommissionEntity =
    vet.Domain.Entities.Commission;

using EmployeeServiceEntity =
    vet.Domain.Entities.EmployeeService;

namespace vet.Application.UseCases.CommissionRule.Commands;

public class UpdateCommissionRuleCommand : IRequest<bool>
{
    public long Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }
}

public class UpdateCommissionRuleCommandHandler
    : IRequestHandler<UpdateCommissionRuleCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCommissionRuleCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        UpdateCommissionRuleCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Type))
            throw new ArgumentException(
                "El tipo de comisión es obligatorio.");

        if (request.ValidFrom == default)
            throw new ArgumentException(
                "La fecha inicial es obligatoria.");

        var type = request.Type.Trim().ToUpperInvariant();

        // 1. Validar reglas fundamentales

        CommissionRules.ValidateRule(
            type, request.Value);

        CommissionRules.ValidateDates(
            request.ValidFrom, request.ValidTo);

        // 2. Buscar regla existente

        var repository = _unitOfWork
            .Repository<CommissionRuleEntity>();

        var rule = await repository
            .GetByIdAsync(request.Id);

        if (rule is null)
            throw new KeyNotFoundException(
                "La regla de comisión no existe.");

        // 3. Verificar asignación

        var employeeService = await _unitOfWork
            .Repository<EmployeeServiceEntity>()
            .GetByIdAsync(rule.EmployeeServiceId);

        if (employeeService is null)
            throw new KeyNotFoundException(
                "La asignación del servicio no existe.");

        // 4. Verificar comisiones generadas

        var commissions = (await _unitOfWork
            .Repository<CommissionEntity>()
            .FindAsync(x => x.RuleId == rule.Id))
            .ToList();

        if (commissions.Any())
        {
            if (rule.Type != type ||
                rule.Value != request.Value ||
                rule.ValidFrom != request.ValidFrom)
            {
                throw new ArgumentException(
                    "No se puede modificar el tipo, valor o fecha inicial " +
                    "de una regla que ya generó comisiones.");
            }

            if (request.ValidTo.HasValue)
            {
                var peruTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "America/Lima");

                var lastCommissionDate = commissions
                    .Select(x => DateOnly.FromDateTime(
                        TimeZoneInfo.ConvertTimeFromUtc(
                            x.Date, peruTimeZone)))
                    .Max();

                if (request.ValidTo.Value < lastCommissionDate)
                    throw new ArgumentException(
                        "La fecha final no puede ser anterior " +
                        "a la última comisión generada.");
            }
        }

        // 5. Validar vigencias excluyendo la regla actual

        var existingRules = await repository
            .FindAsync(x =>
                x.EmployeeServiceId == rule.EmployeeServiceId &&
                x.Id != rule.Id);

        CommissionRules.ValidateOverlappingRules(
            request.ValidFrom,
            request.ValidTo,
            existingRules);

        // 6. Actualizar regla

        rule.Type = type;
        rule.Value = request.Value;
        rule.ValidFrom = request.ValidFrom;
        rule.ValidTo = request.ValidTo;

        // GetByIdAsync devuelve una entidad rastreada

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}