using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.Commission;

using CommissionRuleEntity =
    vet.Domain.Entities.CommissionRule;

using EmployeeServiceEntity =
    vet.Domain.Entities.EmployeeService;

namespace vet.Application.UseCases.CommissionRule.Commands;

public class CreateCommissionRuleCommand : IRequest<long>
{
    public long EmployeeServiceId { get; set; }

    public string Type { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }
}

public class CreateCommissionRuleCommandHandler
    : IRequestHandler<CreateCommissionRuleCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateCommissionRuleCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateCommissionRuleCommand request,
        CancellationToken cancellationToken)
    {
        if (request.EmployeeServiceId <= 0)
            throw new ArgumentException(
                "La asignación del trabajador es obligatoria.");

        if (string.IsNullOrWhiteSpace(request.Type))
            throw new ArgumentException(
                "El tipo de comisión es obligatorio.");

        if (request.ValidFrom == default)
            throw new ArgumentException(
                "La fecha inicial es obligatoria.");

        var type = request.Type.Trim().ToUpperInvariant();

        // 1. Validar reglas fundamentales

        CommissionRules.ValidateRule(
            type,
            request.Value);

        CommissionRules.ValidateDates(
            request.ValidFrom,
            request.ValidTo);

        // 2. Verificar asignación del servicio

        var employeeService = await _unitOfWork
            .Repository<EmployeeServiceEntity>()
            .GetByIdAsync(request.EmployeeServiceId);

        if (employeeService is null)
            throw new KeyNotFoundException(
                "La asignación del servicio no existe.");

        if (!employeeService.IsEnabled)
            throw new ArgumentException(
                "El trabajador no tiene habilitado este servicio.");

        // 3. Verificar vigencias existentes

        var existingRules = await _unitOfWork
            .Repository<CommissionRuleEntity>()
            .FindAsync(r =>
                r.EmployeeServiceId == request.EmployeeServiceId);

        CommissionRules.ValidateOverlappingRules(
            request.ValidFrom,
            request.ValidTo,
            existingRules);

        // 4. Crear regla de comisión

        var rule = new CommissionRuleEntity
        {
            EmployeeServiceId = request.EmployeeServiceId,
            Type = type,
            Value = request.Value,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo
        };

        await _unitOfWork
            .Repository<CommissionRuleEntity>()
            .AddAsync(rule);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return rule.Id;
    }
}