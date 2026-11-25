using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.consultation;
using vet.Domain.Services.Diagnosis;
using DiagnosisEntity = vet.Domain.Entities.Diagnosis;
using ConsultationEntity = vet.Domain.Entities.Consultation;

namespace vet.Application.UseCases.Diagnosis.Commands;

public class UpdateDiagnosisCommand : IRequest<Unit>
{
    public long AppointmentId { get; set; }

    public string Description { get; set; } = string.Empty;
}

public class UpdateDiagnosisCommandHandler
    : IRequestHandler<UpdateDiagnosisCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDiagnosisCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(
        UpdateDiagnosisCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        DiagnosisRules.ValidateDescription(
            request.Description);

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // ========================================
                // 1. OBTENER Y BLOQUEAR CITA
                // ========================================

                var appointment = await _unitOfWork
                    .GetAppointmentForUpdateAsync(
                        request.AppointmentId,
                        ct);

                if (appointment == null)
                    throw new KeyNotFoundException(
                        "La cita no existe.");

                // ========================================
                // 2. OBTENER ATENCIÓN
                // ========================================

                var visits = await _unitOfWork
                    .Repository<Visit>()
                    .FindAsync(v =>
                        v.AppointmentId == appointment.Id);

                var visit = visits.SingleOrDefault();

                if (visit == null)
                    throw new KeyNotFoundException(
                        "La atención no existe.");

                // ========================================
                // 3. VALIDAR ESTADOS
                // ========================================

                ConsultationRules.ValidateUpdate(
                    appointment.Status,
                    visit.Status);

                // ========================================
                // 4. OBTENER CONSULTA
                // ========================================

                var consultations = await _unitOfWork
                    .Repository<ConsultationEntity>()
                    .FindAsync(c => c.VisitId == visit.Id);

                var consultation =
                    consultations.SingleOrDefault();

                if (consultation == null)
                    throw new KeyNotFoundException(
                        "La consulta médica no existe.");

                // ========================================
                // 5. OBTENER DIAGNÓSTICO
                // ========================================

                var diagnoses = await _unitOfWork
                    .Repository<DiagnosisEntity>()
                    .FindAsync(d =>
                        d.ConsultationId == consultation.Id);

                var diagnosis = diagnoses.SingleOrDefault();

                if (diagnosis == null)
                    throw new KeyNotFoundException(
                        "El diagnóstico no existe.");

                // ========================================
                // 6. ACTUALIZAR DIAGNÓSTICO
                // ========================================

                diagnosis.Description =
                    request.Description.Trim();

                _unitOfWork
                    .Repository<DiagnosisEntity>()
                    .Update(diagnosis);

                await _unitOfWork.SaveChangesAsync(ct);
            },
            cancellationToken);

        return Unit.Value;
    }
}