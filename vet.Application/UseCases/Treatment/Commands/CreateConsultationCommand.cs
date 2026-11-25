using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.consultation;
using vet.Domain.Services.Treatment;
using TreatmentEntity = vet.Domain.Entities.Treatment;
using ConsultationEntity = vet.Domain.Entities.Consultation;

namespace vet.Application.UseCases.Treatment.Commands;

public class CreateTreatmentCommand : IRequest<long>
{
    public long AppointmentId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? Dosage { get; set; }

    public string? Frequency { get; set; }

    public string? Duration { get; set; }

    public string? Instructions { get; set; }
}

public class CreateTreatmentCommandHandler
    : IRequestHandler<CreateTreatmentCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateTreatmentCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateTreatmentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        TreatmentRules.ValidateDescription(
            request.Description);

        long treatmentId = 0;

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

                // 4. Obtener consulta médica

                var consultations = await _unitOfWork
                    .Repository<ConsultationEntity>()
                    .FindAsync(c => c.VisitId == visit.Id);

                var consultation =
                    consultations.SingleOrDefault();

                if (consultation == null)
                    throw new ArgumentException(
                        "Primero debe registrar la consulta médica.");

                // 5. Crear tratamiento

                var treatment = new TreatmentEntity
                {
                    ConsultationId = consultation.Id,
                    Description = request.Description.Trim(),
                    Dosage = request.Dosage?.Trim(),
                    Frequency = request.Frequency?.Trim(),
                    Duration = request.Duration?.Trim(),
                    Instructions = request.Instructions?.Trim()
                };

                await _unitOfWork
                    .Repository<TreatmentEntity>()
                    .AddAsync(treatment);

                await _unitOfWork.SaveChangesAsync(ct);

                treatmentId = treatment.Id;
            },
            cancellationToken);

        return treatmentId;
    }
}