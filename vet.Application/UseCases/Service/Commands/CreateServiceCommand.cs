using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using ServiceEntity = vet.Domain.Entities.Service;

namespace vet.Application.UseCases.Service.Commands;

public class CreateServiceCommand : IRequest<long>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsClinical { get; set; }
}

public class CreateServiceCommandHandler
    : IRequestHandler<CreateServiceCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateServiceCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateServiceCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validar reglas fundamentales
        ServiceRules.Validate(
            request.Name,
            request.BasePrice,
            request.DurationMinutes);

        // 2. Comprobar si el servicio ya existe
        var name = request.Name.Trim();

        var existing = await _unitOfWork
            .Repository<ServiceEntity>()
            .FindAsync(s => s.Name.ToLower() == name.ToLower());

        if (existing.Any())
            throw new ArgumentException(
                "Ya existe un servicio con ese nombre.");

        // 3. Crear la entidad
        var service = new ServiceEntity
        {
            Name = name,
            Description = request.Description?.Trim(),
            BasePrice = request.BasePrice,
            DurationMinutes = request.DurationMinutes,
            IsClinical = request.IsClinical,
            IsActive = true
        };

        // 4. Registrar en el repositorio
        await _unitOfWork
            .Repository<ServiceEntity>()
            .AddAsync(service);

        // 5. Guardar cambios
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return service.Id;
    }
}