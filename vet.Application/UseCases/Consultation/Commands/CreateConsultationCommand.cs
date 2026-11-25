using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.consultation;
using ConsultationEntity = vet.Domain.Entities.Consultation;
using VisitServiceEntity = vet.Domain.Entities.VisitService;
using ServiceEntity = vet.Domain.Entities.Service;

namespace vet.Application.UseCases.Consultation.Commands;

public class CreateConsultationCommand : IRequest<long>
{
    public long AppointmentId { get; set; }

    public decimal? WeightKg { get; set; }

    public string? Reason { get; set; }

    public string? Anamnesis { get; set; }

    public string? PhysicalExam { get; set; }

    public string? Instructions { get; set; }
}

public class CreateConsultationCommandHandler
    : IRequestHandler<CreateConsultationCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateConsultationCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateConsultationCommand request,
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

        long consultationId = 0;

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
                    throw new ArgumentException(
                        "La cita no tiene una atención registrada.");

                // 3. Verificar consulta existente

                var consultations = await _unitOfWork
                    .Repository<ConsultationEntity>()
                    .FindAsync(c => c.VisitId == visit.Id);

                ConsultationRules.ValidateCreation(
                    appointment.Status,
                    visit.Status,
                    consultations.Any());

                // 4. Verificar servicios clínicos

                var visitServices = await _unitOfWork
                    .Repository<VisitServiceEntity>()
                    .FindAsync(vs =>
                        vs.VisitId == visit.Id &&
                        vs.Status != "VOIDED");

                var serviceIds = visitServices
                    .Select(vs => vs.ServiceId)
                    .Distinct()
                    .ToList();

                var clinicalServices = await _unitOfWork
                    .Repository<ServiceEntity>()
                    .FindAsync(s =>
                        serviceIds.Contains(s.Id) &&
                        s.IsClinical);

                ConsultationRules.ValidateClinicalService(
                    clinicalServices.Any());

                // 5. Registrar consulta médica

                var consultation = new ConsultationEntity
                {
                    VisitId = visit.Id,
                    VeterinarianId = appointment.EmployeeId,
                    Date = DateTime.UtcNow,
                    WeightKg = request.WeightKg,
                    Reason = request.Reason?.Trim(),
                    Anamnesis = request.Anamnesis?.Trim(),
                    PhysicalExam = request.PhysicalExam?.Trim(),
                    Instructions = request.Instructions?.Trim()
                };

                await _unitOfWork
                    .Repository<ConsultationEntity>()
                    .AddAsync(consultation);

                await _unitOfWork.SaveChangesAsync(ct);

                consultationId = consultation.Id;
            },
            cancellationToken);

        return consultationId;
    }
}