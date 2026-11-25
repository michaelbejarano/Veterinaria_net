using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;

namespace vet.Application.UseCases.Appointment.Commands;

public class StartAppointmentCommand : IRequest<long>
{
    public long AppointmentId { get; set; }
}

public class StartAppointmentCommandHandler
    : IRequestHandler<StartAppointmentCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public StartAppointmentCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        StartAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        long visitId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Buscar y bloquear la cita
                var appointment = await _unitOfWork
                    .GetAppointmentForUpdateAsync(
                        request.AppointmentId, ct);

                if (appointment is null)
                    throw new KeyNotFoundException(
                        "La cita no existe.");

                // 2. Validar estado de la cita
                AppointmentRules.ValidateStart(
                    appointment.Status);

                // 3. Verificar que no exista otra atención
                var existingVisits = await _unitOfWork
                    .Repository<Visit>()
                    .FindAsync(v =>
                        v.AppointmentId == appointment.Id);

                if (existingVisits.Any())
                    throw new ArgumentException(
                        "La cita ya tiene una atención registrada.");

                // 4. Obtener los servicios reservados
                var appointmentServices = (
                    await _unitOfWork
                        .Repository<AppointmentService>()
                        .FindAsync(s =>
                            s.AppointmentId == appointment.Id)
                ).ToList();

                if (appointmentServices.Count == 0)
                    throw new ArgumentException(
                        "La cita no tiene servicios registrados.");

                // 5. Crear la atención
                var visit = new Visit
                {
                    AppointmentId = appointment.Id,
                    PetId = appointment.PetId,
                    StartAt = DateTime.UtcNow,
                    EndAt = null,
                    Status = "IN_PROGRESS"
                };

                // 6. Copiar los servicios reservados
                foreach (var service in appointmentServices)
                {
                    visit.VisitServices.Add(
                        new Domain.Entities.VisitService
                        {
                            ServiceId = service.ServiceId,
                            EmployeeId = appointment.EmployeeId,
                            FinalPrice = service.AgreedPrice,
                            Status = "PENDING"
                        });
                }

                // 7. Registrar la atención y sus servicios
                await _unitOfWork
                    .Repository<Visit>()
                    .AddAsync(visit);

                // 8. Actualizar el estado de la cita
                appointment.Status = "IN_PROGRESS";

                // 9. Guardar todos los cambios
                await _unitOfWork.SaveChangesAsync(ct);

                visitId = visit.Id;

            }, cancellationToken);

        return visitId;
    }
}