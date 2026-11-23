using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using AppointmentEntity = vet.Domain.Entities.Appointment;

namespace vet.Application.UseCases.Appointment.Commands;

public class CancelAppointmentCommand : IRequest<bool>
{
    public long AppointmentId { get; set; }
}

public class CancelAppointmentCommandHandler
    : IRequestHandler<CancelAppointmentCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelAppointmentCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        CancelAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar la cita
        var appointment = await _unitOfWork
            .Repository<AppointmentEntity>()
            .GetByIdAsync(request.AppointmentId);

        if (appointment is null)
            throw new KeyNotFoundException(
                "La cita no existe.");

        // 2. Validar si puede cancelarse
        AppointmentRules.ValidateCancellation(
            appointment.Status);

        // 3. Cambiar el estado
        appointment.Status = "CANCELLED";

        // 4. Guardar los cambios
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}