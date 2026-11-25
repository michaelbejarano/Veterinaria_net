using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;

using VisitServiceEntity = vet.Domain.Entities.VisitService;
using ServiceEntity = vet.Domain.Entities.Service;

namespace vet.Application.UseCases.VisitService.Commands;

public class AddVisitServiceCommand
    : IRequest<AddVisitServiceResponse>
{
    public long AppointmentId { get; set; }

    public long ServiceId { get; set; }
}

public class AddVisitServiceResponse
{
    public long VisitServiceId { get; set; }

    public long VisitId { get; set; }

    public long ServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public long EmployeeId { get; set; }

    public decimal FinalPrice { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class AddVisitServiceCommandHandler
    : IRequestHandler<
        AddVisitServiceCommand,
        AddVisitServiceResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddVisitServiceCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AddVisitServiceResponse> Handle(
        AddVisitServiceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        if (request.ServiceId <= 0)
            throw new ArgumentException(
                "El servicio es obligatorio.");

        AddVisitServiceResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // ========================================
                // 1. BLOQUEAR CITA
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
                    throw new ArgumentException(
                        "La cita no tiene una atención registrada.");

                VisitServiceRules.ValidateAddition(
                    appointment.Status,
                    visit.Status);

                // ========================================
                // 3. VALIDAR SERVICIO
                // ========================================

                var service = await _unitOfWork
                    .Repository<ServiceEntity>()
                    .GetByIdAsync(request.ServiceId);

                if (service == null || !service.IsActive)
                    throw new ArgumentException(
                        "El servicio no existe o está inactivo.");

                VisitServiceRules.ValidateFinalPrice(
                    service.BasePrice);

                // ========================================
                // 4. VALIDAR TRABAJADOR
                // ========================================

                var employeeServices = await _unitOfWork
                    .Repository<Domain.Entities.EmployeeService>()
                    .FindAsync(es =>
                        es.EmployeeId == appointment.EmployeeId &&
                        es.ServiceId == service.Id &&
                        es.IsEnabled);

                if (!employeeServices.Any())
                    throw new ArgumentException(
                        "El trabajador no está habilitado para realizar este servicio.");

                // ========================================
                // 5. EVITAR SERVICIOS DUPLICADOS
                // ========================================

                var existingServices = await _unitOfWork
                    .Repository<VisitServiceEntity>()
                    .FindAsync(vs =>
                        vs.VisitId == visit.Id &&
                        vs.ServiceId == service.Id &&
                        vs.Status != "VOIDED");

                if (existingServices.Any())
                    throw new ArgumentException(
                        "El servicio ya está registrado en esta atención.");

                // ========================================
                // 6. REGISTRAR SERVICIO ADICIONAL
                // ========================================

                var visitService = new VisitServiceEntity
                {
                    VisitId = visit.Id,
                    ServiceId = service.Id,
                    EmployeeId = appointment.EmployeeId,
                    FinalPrice = service.BasePrice,
                    Status = "PENDING"
                };

                await _unitOfWork
                    .Repository<VisitServiceEntity>()
                    .AddAsync(visitService);

                await _unitOfWork.SaveChangesAsync(ct);

                // ========================================
                // 7. PREPARAR RESPUESTA
                // ========================================

                response = new AddVisitServiceResponse
                {
                    VisitServiceId = visitService.Id,
                    VisitId = visit.Id,
                    ServiceId = service.Id,
                    ServiceName = service.Name,
                    EmployeeId = visitService.EmployeeId,
                    FinalPrice = visitService.FinalPrice,
                    Status = visitService.Status
                };
            },
            cancellationToken);

        return response!;
    }
}