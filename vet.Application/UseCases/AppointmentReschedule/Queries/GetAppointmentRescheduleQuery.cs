using MediatR;
using vet.Domain.Ports;

using AppointmentEntity =
    vet.Domain.Entities.Appointment;

using AppointmentRescheduleEntity =
    vet.Domain.Entities.AppointmentReschedule;

namespace vet.Application.UseCases.AppointmentReschedule.Queries;

// DTO de respuesta

public class AppointmentRescheduleDto
{
    public long Id { get; set; }

    public long AppointmentId { get; set; }

    public DateTime PreviousStart { get; set; }

    public DateTime PreviousEnd { get; set; }

    public DateTime NewStart { get; set; }

    public DateTime NewEnd { get; set; }

    public decimal Fee { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? PaymentExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

// Query

public class GetAppointmentRescheduleQuery
    : IRequest<List<AppointmentRescheduleDto>>
{
    public long AppointmentId { get; set; }
}

// Handler

public class GetAppointmentRescheduleQueryHandler
    : IRequestHandler<
        GetAppointmentRescheduleQuery,
        List<AppointmentRescheduleDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAppointmentRescheduleQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AppointmentRescheduleDto>> Handle(
        GetAppointmentRescheduleQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar que la cita exista

        var appointment = await _unitOfWork
            .Repository<AppointmentEntity>()
            .GetByIdAsync(request.AppointmentId);

        if (appointment is null)
            throw new KeyNotFoundException(
                "La cita no existe.");

        // 2. Consultar reprogramaciones de la cita

        var reschedules = await _unitOfWork
            .Repository<AppointmentRescheduleEntity>()
            .FindAsync(x =>
                x.AppointmentId == request.AppointmentId);

        // 3. Ordenar y mapear al DTO

        return reschedules
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new AppointmentRescheduleDto
            {
                Id = x.Id,
                AppointmentId = x.AppointmentId,
                PreviousStart = x.PreviousStart,
                PreviousEnd = x.PreviousEnd,
                NewStart = x.NewStart,
                NewEnd = x.NewEnd,
                Fee = x.Fee,
                Status = x.Status,
                PaymentExpiresAt = x.PaymentExpiresAt,
                CreatedAt = x.CreatedAt
            })
            .ToList();
    }
}