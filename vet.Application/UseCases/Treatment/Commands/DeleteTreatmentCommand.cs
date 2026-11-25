using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.consultation;
using TreatmentEntity = vet.Domain.Entities.Treatment;
using ConsultationEntity = vet.Domain.Entities.Consultation;

namespace vet.Application.UseCases.Treatment.Commands;

public class DeleteTreatmentCommand : IRequest<Unit>
{
    public long AppointmentId { get; set; }

    public long TreatmentId { get; set; }
}

public class DeleteTreatmentCommandHandler
    : IRequestHandler<DeleteTreatmentCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTreatmentCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(
        DeleteTreatmentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        if (request.TreatmentId <= 0)
            throw new ArgumentException(
                "El tratamiento es obligatorio.");

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Obtener y bloquear cita

                var appointment = await _unitOfWork
                    .GetAppointmentForUpdateAsync(
                        request.AppointmentId,
                        ct);

                if (appointment == null)
                    throw new KeyNotFoundException(
                        "La cita no existe.");

                // 2. Obtener atención

                var visits = await _unitOfWork
                    .Repository<Visit>()
                    .FindAsync(v =>
                        v.AppointmentId == appointment.Id);

                var visit = visits.SingleOrDefault();

                if (visit == null)
                    throw new KeyNotFoundException(
                        "La atención no existe.");

                // 3. Validar estados

                ConsultationRules.ValidateUpdate(
                    appointment.Status,
                    visit.Status);

                // 4. Obtener consulta

                var consultations = await _unitOfWork
                    .Repository<ConsultationEntity>()
                    .FindAsync(c => c.VisitId == visit.Id);

                var consultation =
                    consultations.SingleOrDefault();

                if (consultation == null)
                    throw new KeyNotFoundException(
                        "La consulta médica no existe.");

                // 5. Obtener tratamiento

                var treatment = await _unitOfWork
                    .Repository<TreatmentEntity>()
                    .GetByIdAsync(request.TreatmentId);

                if (treatment == null ||
                    treatment.ConsultationId != consultation.Id)
                    throw new KeyNotFoundException(
                        "El tratamiento no pertenece a esta consulta.");

                // 6. Eliminar tratamiento

                _unitOfWork
                    .Repository<TreatmentEntity>()
                    .Delete(treatment);

                await _unitOfWork.SaveChangesAsync(ct);
            },
            cancellationToken);

        return Unit.Value;
    }
}