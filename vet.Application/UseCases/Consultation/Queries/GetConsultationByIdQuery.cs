using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;

using ConsultationEntity = vet.Domain.Entities.Consultation;
using DiagnosisEntity = vet.Domain.Entities.Diagnosis;
using TreatmentEntity = vet.Domain.Entities.Treatment;
using AppointmentEntity = vet.Domain.Entities.Appointment;

namespace vet.Application.UseCases.Consultation.Queries;

public class GetConsultationByAppointmentQuery
    : IRequest<ConsultationDetailResponse>
{
    public long AppointmentId { get; set; }
}

public class ConsultationDetailResponse
{
    public long Id { get; set; }
    public long AppointmentId { get; set; }
    public long VisitId { get; set; }
    public long PetId { get; set; }
    public long VeterinarianId { get; set; }

    public DateTime Date { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Reason { get; set; }
    public string? Anamnesis { get; set; }
    public string? PhysicalExam { get; set; }
    public string? Instructions { get; set; }

    public string VisitStatus { get; set; } = string.Empty;

    public DiagnosisDetailResponse? Diagnosis { get; set; }

    public List<TreatmentDetailResponse> Treatments { get; set; }
        = new();
}

public class DiagnosisDetailResponse
{
    public long Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class TreatmentDetailResponse
{
    public long Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }
}

public class GetConsultationByAppointmentQueryHandler
    : IRequestHandler<
        GetConsultationByAppointmentQuery,
        ConsultationDetailResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConsultationByAppointmentQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConsultationDetailResponse> Handle(
        GetConsultationByAppointmentQuery request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        // 1. Obtener cita

        var appointment = await _unitOfWork
            .Repository<AppointmentEntity>()
            .GetByIdAsync(request.AppointmentId);

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

        // 3. Obtener consulta médica

        var consultations = await _unitOfWork
            .Repository<ConsultationEntity>()
            .FindAsync(c => c.VisitId == visit.Id);

        var consultation =
            consultations.SingleOrDefault();

        if (consultation == null)
            throw new KeyNotFoundException(
                "La consulta médica no existe.");

        // 4. Obtener diagnóstico

        var diagnoses = await _unitOfWork
            .Repository<DiagnosisEntity>()
            .FindAsync(d =>
                d.ConsultationId == consultation.Id);

        var diagnosis = diagnoses.SingleOrDefault();

        // 5. Obtener tratamientos

        var treatments = await _unitOfWork
            .Repository<TreatmentEntity>()
            .FindAsync(t =>
                t.ConsultationId == consultation.Id);

        // 6. Construir respuesta

        return new ConsultationDetailResponse
        {
            Id = consultation.Id,
            AppointmentId = appointment.Id,
            VisitId = visit.Id,
            PetId = visit.PetId,
            VeterinarianId = consultation.VeterinarianId,

            Date = consultation.Date,
            WeightKg = consultation.WeightKg,
            Reason = consultation.Reason,
            Anamnesis = consultation.Anamnesis,
            PhysicalExam = consultation.PhysicalExam,
            Instructions = consultation.Instructions,

            VisitStatus = visit.Status,

            Diagnosis = diagnosis == null
                ? null
                : new DiagnosisDetailResponse
                {
                    Id = diagnosis.Id,
                    Description = diagnosis.Description,
                    IsPrimary = diagnosis.IsPrimary
                },

            Treatments = treatments
                .OrderBy(t => t.Id)
                .Select(t => new TreatmentDetailResponse
                {
                    Id = t.Id,
                    Description = t.Description,
                    Dosage = t.Dosage,
                    Frequency = t.Frequency,
                    Duration = t.Duration,
                    Instructions = t.Instructions
                })
                .ToList()
        };
    }
}