using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;

using VisitServiceEntity = vet.Domain.Entities.VisitService;
using ServiceEntity = vet.Domain.Entities.Service;
using ConsultationEntity = vet.Domain.Entities.Consultation;
using AppointmentEntity = vet.Domain.Entities.Appointment;

namespace vet.Application.UseCases.Appointment.Commands;

public class CompleteAppointmentCommand
    : IRequest<CompleteAppointmentResponse>
{
    public long AppointmentId { get; set; }
}

public class CompleteAppointmentResponse
{
    public long AppointmentId { get; set; }
    public long VisitId { get; set; }
    public string Status { get; set; } = string.Empty;

    public DateTime EndAt { get; set; }

    public int CompletedServices { get; set; }
    public int VoidedServices { get; set; }

    public decimal TotalServices { get; set; }
}

public class CompleteAppointmentCommandHandler
    : IRequestHandler<
        CompleteAppointmentCommand,
        CompleteAppointmentResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CompleteAppointmentCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CompleteAppointmentResponse> Handle(
        CompleteAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        CompleteAppointmentResponse? response = null;

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

                // 3. Obtener servicios de la atención

                var visitServices = (await _unitOfWork
                    .Repository<VisitServiceEntity>()
                    .FindAsync(vs => vs.VisitId == visit.Id))
                    .ToList();

                var completedServices = visitServices
                    .Where(vs => vs.Status == "COMPLETED")
                    .ToList();

                var hasPendingServices = visitServices
                    .Any(vs => vs.Status == "PENDING");

                // 4. Verificar servicios clínicos realizados

                var completedServiceIds = completedServices
                    .Select(vs => vs.ServiceId)
                    .Distinct()
                    .ToList();

                var clinicalServices = await _unitOfWork
                    .Repository<ServiceEntity>()
                    .FindAsync(s =>
                        completedServiceIds.Contains(s.Id)
                        && s.IsClinical);

                var requiresConsultation =
                    clinicalServices.Any();

                // 5. Verificar consulta médica

                var consultations = await _unitOfWork
                    .Repository<ConsultationEntity>()
                    .FindAsync(c => c.VisitId == visit.Id);

                // 6. Validar reglas de negocio

                AppointmentRules.ValidateCompletion(
                    appointment.Status,
                    visit.Status,
                    hasPendingServices,
                    completedServices.Any(),
                    requiresConsultation,
                    consultations.Any());

                // 7. Finalizar atención

                var now = DateTime.UtcNow;

                visit.Status = "COMPLETED";
                visit.EndAt = now;

                appointment.Status = "COMPLETED";

                _unitOfWork
                    .Repository<Visit>()
                    .Update(visit);

                _unitOfWork
                    .Repository<AppointmentEntity>()
                    .Update(appointment);

                // 8. Guardar cambios

                await _unitOfWork.SaveChangesAsync(ct);

                // 9. Construir respuesta

                response = new CompleteAppointmentResponse
                {
                    AppointmentId = appointment.Id,
                    VisitId = visit.Id,
                    Status = appointment.Status,
                    EndAt = now,

                    CompletedServices =
                        completedServices.Count,

                    VoidedServices = visitServices
                        .Count(vs => vs.Status == "VOIDED"),

                    TotalServices = completedServices
                        .Sum(vs => vs.FinalPrice)
                };
            },
            cancellationToken);

        return response!;
    }
}