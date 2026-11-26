using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.ScheduleBlock;
using ScheduleBlockEntity = vet.Domain.Entities.ScheduleBlock;
using EmployeeEntity = vet.Domain.Entities.Employee;
using AppointmentEntity = vet.Domain.Entities.Appointment;

namespace vet.Application.UseCases.ScheduleBlock.Commands;

public class CreateScheduleBlockCommand : IRequest<long>
{
    public long EmployeeId { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string? Reason { get; set; }
}

public class CreateScheduleBlockCommandHandler
    : IRequestHandler<CreateScheduleBlockCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateScheduleBlockCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateScheduleBlockCommand request,
        CancellationToken cancellationToken)
    {
        if (request.EmployeeId <= 0)
            throw new ArgumentException(
                "El trabajador es obligatorio.");

        var startUtc = request.StartAt.UtcDateTime;
        var endUtc = request.EndAt.UtcDateTime;

        ScheduleBlockRules.ValidateDates(
            startUtc,
            endUtc);

        if (startUtc <= DateTime.UtcNow)
            throw new ArgumentException(
                "El bloqueo debe comenzar en una fecha futura.");

        // 1. Verificar trabajador

        var employee = await _unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.EmployeeId);

        if (employee is null)
            throw new KeyNotFoundException(
                "El trabajador no existe.");

        if (!employee.IsActive)
            throw new ArgumentException(
                "El trabajador está inactivo.");

        // 2. Verificar bloqueos existentes

        var blocks = await _unitOfWork
            .Repository<ScheduleBlockEntity>()
            .FindAsync(b =>
                b.EmployeeId == request.EmployeeId &&
                b.StartAt < endUtc &&
                b.EndAt > startUtc);

        ScheduleBlockRules.ValidateBlockConflicts(
            startUtc,
            endUtc,
            blocks);

        // 3. Verificar citas existentes

        var appointments = await _unitOfWork
            .Repository<AppointmentEntity>()
            .FindAsync(a =>
                a.EmployeeId == request.EmployeeId &&
                a.StartAt < endUtc &&
                a.EndAt > startUtc);

        ScheduleBlockRules.ValidateAppointmentConflicts(
            startUtc,
            endUtc,
            appointments);

        // 4. Crear bloqueo

        var block = new ScheduleBlockEntity
        {
            EmployeeId = request.EmployeeId,
            StartAt = startUtc,
            EndAt = endUtc,

            Reason = string.IsNullOrWhiteSpace(request.Reason)
                ? null
                : request.Reason.Trim()
        };

        await _unitOfWork
            .Repository<ScheduleBlockEntity>()
            .AddAsync(block);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return block.Id;
    }
}