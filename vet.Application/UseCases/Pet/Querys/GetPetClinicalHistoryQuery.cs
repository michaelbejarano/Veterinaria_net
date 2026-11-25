using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Application.UseCases.Consultation.Queries;

using PetEntity = vet.Domain.Entities.Pet;
using ConsultationEntity = vet.Domain.Entities.Consultation;
using DiagnosisEntity = vet.Domain.Entities.Diagnosis;
using TreatmentEntity = vet.Domain.Entities.Treatment;
using VisitServiceEntity = vet.Domain.Entities.VisitService;
using ServiceEntity = vet.Domain.Entities.Service;

namespace vet.Application.UseCases.Pet.Queries;

public class GetPetClinicalHistoryQuery
    : IRequest<PetClinicalHistoryResponse>
{
    public long PetId { get; set; }
}

public class PetClinicalHistoryResponse
{
    public long PetId { get; set; }

    public int TotalConsultations { get; set; }

    public List<ClinicalHistoryItemResponse> History { get; set; }
        = new();
}

public class ClinicalHistoryItemResponse
{
    public long ConsultationId { get; set; }

    public long? AppointmentId { get; set; }

    public long VisitId { get; set; }

    public long VeterinarianId { get; set; }

    public DateTime Date { get; set; }

    public decimal? WeightKg { get; set; }

    public string? Reason { get; set; }

    public string? Anamnesis { get; set; }

    public string? PhysicalExam { get; set; }

    public string? Instructions { get; set; }

    public DiagnosisDetailResponse? Diagnosis { get; set; }

    public List<TreatmentDetailResponse> Treatments { get; set; }
        = new();

    public List<ClinicalHistoryServiceResponse> Services { get; set; }
        = new();
}

public class ClinicalHistoryServiceResponse
{
    public long VisitServiceId { get; set; }

    public long ServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public long EmployeeId { get; set; }
}

public class GetPetClinicalHistoryQueryHandler
    : IRequestHandler<
        GetPetClinicalHistoryQuery,
        PetClinicalHistoryResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPetClinicalHistoryQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PetClinicalHistoryResponse> Handle(
        GetPetClinicalHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (request.PetId <= 0)
            throw new ArgumentException(
                "La mascota es obligatoria.");

        // ========================================
        // 1. VERIFICAR MASCOTA
        // ========================================

        var pet = await _unitOfWork
            .Repository<PetEntity>()
            .GetByIdAsync(request.PetId);

        if (pet == null)
            throw new KeyNotFoundException(
                "La mascota no existe.");

        var response = new PetClinicalHistoryResponse
        {
            PetId = pet.Id
        };

        // ========================================
        // 2. OBTENER ATENCIONES FINALIZADAS
        // ========================================

        var visits = (await _unitOfWork
            .Repository<Visit>()
            .FindAsync(v =>
                v.PetId == pet.Id &&
                v.Status == "COMPLETED"))
            .ToList();

        if (!visits.Any())
            return response;

        var visitIds = visits
            .Select(v => v.Id)
            .ToList();

        // ========================================
        // 3. OBTENER CONSULTAS MÉDICAS
        // ========================================

        var consultations = (await _unitOfWork
            .Repository<ConsultationEntity>()
            .FindAsync(c =>
                visitIds.Contains(c.VisitId)))
            .ToList();

        if (!consultations.Any())
            return response;

        var consultationIds = consultations
            .Select(c => c.Id)
            .ToList();

        // ========================================
        // 4. OBTENER DIAGNÓSTICOS
        // ========================================

        var diagnoses = (await _unitOfWork
            .Repository<DiagnosisEntity>()
            .FindAsync(d =>
                consultationIds.Contains(d.ConsultationId)))
            .ToList();

        // ========================================
        // 5. OBTENER TRATAMIENTOS
        // ========================================

        var treatments = (await _unitOfWork
            .Repository<TreatmentEntity>()
            .FindAsync(t =>
                consultationIds.Contains(t.ConsultationId)))
            .ToList();

        // ========================================
        // 6. OBTENER SERVICIOS REALIZADOS
        // ========================================

        var visitServices = (await _unitOfWork
            .Repository<VisitServiceEntity>()
            .FindAsync(vs =>
                visitIds.Contains(vs.VisitId) &&
                vs.Status == "COMPLETED"))
            .ToList();

        var serviceIds = visitServices
            .Select(vs => vs.ServiceId)
            .Distinct()
            .ToList();

        var services = (await _unitOfWork
            .Repository<ServiceEntity>()
            .FindAsync(s =>
                serviceIds.Contains(s.Id)))
            .ToDictionary(s => s.Id);

        // ========================================
        // 7. AGRUPAR INFORMACIÓN
        // ========================================

        var visitsById = visits
            .ToDictionary(v => v.Id);

        var diagnosesByConsultation = diagnoses
            .ToDictionary(d => d.ConsultationId);

        var treatmentsByConsultation = treatments
            .ToLookup(t => t.ConsultationId);

        var servicesByVisit = visitServices
            .ToLookup(vs => vs.VisitId);

        // ========================================
        // 8. CONSTRUIR HISTORIAL CLÍNICO
        // ========================================

        foreach (var consultation in consultations
                     .OrderByDescending(c => c.Date)
                     .ThenByDescending(c => c.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var visit = visitsById[consultation.VisitId];

            diagnosesByConsultation.TryGetValue(
                consultation.Id,
                out var diagnosis);

            var item = new ClinicalHistoryItemResponse
            {
                ConsultationId = consultation.Id,
                AppointmentId = visit.AppointmentId,
                VisitId = visit.Id,
                VeterinarianId = consultation.VeterinarianId,

                Date = consultation.Date,
                WeightKg = consultation.WeightKg,
                Reason = consultation.Reason,
                Anamnesis = consultation.Anamnesis,
                PhysicalExam = consultation.PhysicalExam,
                Instructions = consultation.Instructions,

                Diagnosis = diagnosis == null
                    ? null
                    : new DiagnosisDetailResponse
                    {
                        Id = diagnosis.Id,
                        Description = diagnosis.Description,
                        IsPrimary = diagnosis.IsPrimary
                    },

                Treatments = treatmentsByConsultation[
                        consultation.Id]
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
                    .ToList(),

                Services = servicesByVisit[visit.Id]
                    .OrderBy(vs => vs.Id)
                    .Select(vs => new ClinicalHistoryServiceResponse
                    {
                        VisitServiceId = vs.Id,
                        ServiceId = vs.ServiceId,
                        ServiceName = services.TryGetValue(
                            vs.ServiceId,
                            out var service)
                                ? service.Name
                                : "Servicio no disponible",

                        EmployeeId = vs.EmployeeId
                    })
                    .ToList()
            };

            response.History.Add(item);
        }

        response.TotalConsultations = response.History.Count;

        return response;
    }
}