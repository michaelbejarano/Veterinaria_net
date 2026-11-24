using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using ServiceEntity = vet.Domain.Entities.Service;

namespace vet.Application.UseCases.Service.Commands;

public class UpdateServiceCommand : IRequest<bool>
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsClinical { get; set; }
}

public class UpdateServiceCommandHandler
    : IRequestHandler<UpdateServiceCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateServiceCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        UpdateServiceCommand request,
        CancellationToken cancellationToken)
    {
        var service = await _unitOfWork
            .Repository<ServiceEntity>()
            .GetByIdAsync(request.Id);

        if (service is null)
            return false;

        ServiceRules.Validate(
            request.Name,
            request.BasePrice,
            request.DurationMinutes);

        var name = request.Name.Trim();

        var duplicates = await _unitOfWork
            .Repository<ServiceEntity>()
            .FindAsync(s =>
                s.Id != request.Id &&
                s.Name.ToLower() == name.ToLower());

        if (duplicates.Any())
            throw new ArgumentException(
                "Ya existe otro servicio con ese nombre.");

        service.Name = name;
        service.Description = request.Description?.Trim();
        service.BasePrice = request.BasePrice;
        service.DurationMinutes = request.DurationMinutes;
        service.IsClinical = request.IsClinical;

        _unitOfWork.Repository<ServiceEntity>().Update(service);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}