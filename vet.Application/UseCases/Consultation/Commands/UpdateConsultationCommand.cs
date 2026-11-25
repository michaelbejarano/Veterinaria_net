using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.consultation;
using ConsultationEntity = vet.Domain.Entities.Consultation;

namespace vet.Application.UseCases.Consultation.Commands;

public class UpdateConsultationCommand : IRequest<Unit>
{
    public long AppointmentId { get; set; }

    public decimal? WeightKg { get; set; }

    public string? Reason { get; set; }

    public string? Anamnesis { get; set; }

    public string? PhysicalExam { get; set; }

    public string? Instructions { get; set; }
}

public class UpdateConsultationCommandHandler
    : IRequestHandler<UpdateConsultationCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateConsultationCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(
        UpdateConsultationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        ConsultationRules.ValidateWeight(
            request.WeightKg);

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException(
                "El motivo de la consulta es obligatorio.");

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Obtener y bloquear la cita

                var appointment = await _unitOfWork
                    .GetAppointmentForUpdateAsync(
                        request.AppointmentId,
                        ct);

                if (appointment == null)
                    throw new KeyNotFoundException(
                        "La cita no existe.");

                // 2. Obtener la atención

                var visits = await _unitOfWork
                    .Repository<Visit>()
                    .FindAsync(v =>
                        v.AppointmentId == appointment.Id);

                var visit = visits.SingleOrDefault();

                if (visit == null)
                    throw new KeyNotFoundException(
                        "La atención no existe.");

                // 3. Validar estado

                ConsultationRules.ValidateUpdate(
                    appointment.Status,
                    visit.Status);

                // 4. Obtener consulta médica

                var consultations = await _unitOfWork
                    .Repository<ConsultationEntity>()
                    .FindAsync(c => c.VisitId == visit.Id);

                var consultation =
                    consultations.SingleOrDefault();

                if (consultation == null)
                    throw new KeyNotFoundException(
                        "La consulta médica no existe.");

                // 5. Actualizar información clínica

                consultation.WeightKg = request.WeightKg;
                consultation.Reason = request.Reason?.Trim();
                consultation.Anamnesis = request.Anamnesis?.Trim();
                consultation.PhysicalExam = request.PhysicalExam?.Trim();
                consultation.Instructions = request.Instructions?.Trim();

                // 6. Guardar cambios

                _unitOfWork
                    .Repository<ConsultationEntity>()
                    .Update(consultation);

                await _unitOfWork.SaveChangesAsync(ct);
            },
            cancellationToken);

        return Unit.Value;
    }
}