using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;

using VisitServiceEntity = vet.Domain.Entities.VisitService;

namespace vet.Application.UseCases.VisitService.Commands;

public class CancelVisitServiceCommand : IRequest<Unit>
{
    public long AppointmentId { get; set; }

    public long VisitServiceId { get; set; }
}

public class CancelVisitServiceCommandHandler
    : IRequestHandler<CancelVisitServiceCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelVisitServiceCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(
        CancelVisitServiceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        if (request.VisitServiceId <= 0)
            throw new ArgumentException(
                "El servicio es obligatorio.");

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Bloquear y validar la cita

                var appointment = await _unitOfWork
                    .GetAppointmentForUpdateAsync(
                        request.AppointmentId,
                        ct);

                if (appointment == null)
                    throw new KeyNotFoundException(
                        "La cita no existe.");

                if (appointment.Status != "IN_PROGRESS")
                    throw new ArgumentException(
                        "La cita debe estar en curso.");

                // 2. Obtener la atención

                var visits = await _unitOfWork
                    .Repository<Visit>()
                    .FindAsync(v =>
                        v.AppointmentId == appointment.Id);

                var visit = visits.SingleOrDefault();

                if (visit == null)
                    throw new ArgumentException(
                        "La cita no tiene una atención registrada.");

                // 3. Obtener el servicio

                var visitService = await _unitOfWork
                    .Repository<VisitServiceEntity>()
                    .GetByIdAsync(request.VisitServiceId);

                if (visitService == null ||
                    visitService.VisitId != visit.Id)
                    throw new ArgumentException(
                        "El servicio no pertenece a esta atención.");

                // 4. Aplicar reglas de dominio

                VisitServiceRules.ValidateCancellation(
                    visit.Status,
                    visitService.Status);

                // 5. Anular el servicio

                visitService.Status = "VOIDED";

                _unitOfWork
                    .Repository<VisitServiceEntity>()
                    .Update(visitService);

                await _unitOfWork.SaveChangesAsync(ct);
            },
            cancellationToken);

        return Unit.Value;
    }
}