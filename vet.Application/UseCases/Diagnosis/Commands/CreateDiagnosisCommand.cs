using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.consultation;
using vet.Domain.Services.Diagnosis;
using DiagnosisEntity = vet.Domain.Entities.Diagnosis;
using ConsultationEntity = vet.Domain.Entities.Consultation;

namespace vet.Application.UseCases.Diagnosis.Commands;

public class CreateDiagnosisCommand : IRequest<long>
{
    public long AppointmentId { get; set; }

    public string Description { get; set; } = string.Empty;
}

public class CreateDiagnosisCommandHandler
    : IRequestHandler<CreateDiagnosisCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateDiagnosisCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateDiagnosisCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        DiagnosisRules.ValidateDescription(
            request.Description);

        long diagnosisId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // ========================================
                // 1. OBTENER Y BLOQUEAR LA CITA
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
                // 4. OBTENER CONSULTA MÉDICA
                // ========================================

                var consultations = await _unitOfWork
                    .Repository<ConsultationEntity>()
                    .FindAsync(c => c.VisitId == visit.Id);

                var consultation =
                    consultations.SingleOrDefault();

                if (consultation == null)
                    throw new ArgumentException(
                        "Primero debe registrar la consulta médica.");

                // ========================================
                // 5. VALIDAR DIAGNÓSTICO EXISTENTE
                // ========================================

                var diagnoses = await _unitOfWork
                    .Repository<DiagnosisEntity>()
                    .FindAsync(d =>
                        d.ConsultationId == consultation.Id);

                DiagnosisRules.ValidateCreation(
                    diagnoses.Any());

                // ========================================
                // 6. REGISTRAR DIAGNÓSTICO
                // ========================================

                var diagnosis = new DiagnosisEntity
                {
                    ConsultationId = consultation.Id,
                    Description = request.Description.Trim(),
                    IsPrimary = true
                };

                await _unitOfWork
                    .Repository<DiagnosisEntity>()
                    .AddAsync(diagnosis);

                await _unitOfWork.SaveChangesAsync(ct);

                diagnosisId = diagnosis.Id;
            },
            cancellationToken);

        return diagnosisId;
    }
}