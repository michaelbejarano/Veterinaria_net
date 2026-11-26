using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.ScheduleBlock;
using ScheduleBlockEntity = vet.Domain.Entities.ScheduleBlock;
using EmployeeEntity = vet.Domain.Entities.Employee;
using AppointmentEntity = vet.Domain.Entities.Appointment;

namespace vet.Application.UseCases.ScheduleBlock.Commands;

public class UpdateScheduleBlockCommand : IRequest<Unit>
{
    public long Id { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string? Reason { get; set; }
}

public class UpdateScheduleBlockCommandHandler
    : IRequestHandler<UpdateScheduleBlockCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateScheduleBlockCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(
        UpdateScheduleBlockCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
            throw new ArgumentException(
                "El bloqueo es obligatorio.");

        var startUtc = request.StartAt.UtcDateTime;
        var endUtc = request.EndAt.UtcDateTime;

        ScheduleBlockRules.ValidateDates(
            startUtc,
            endUtc);

        // 1. Buscar bloqueo

        var block = await _unitOfWork
            .Repository<ScheduleBlockEntity>()
            .GetByIdAsync(request.Id);

        if (block is null)
            throw new KeyNotFoundException(
                "El bloqueo no existe.");

        if (block.StartAt <= DateTime.UtcNow)
            throw new ArgumentException(
                "No se puede modificar un bloqueo que ya comenzó.");

        if (startUtc <= DateTime.UtcNow)
            throw new ArgumentException(
                "El nuevo horario debe comenzar en una fecha futura.");

        // 2. Verificar trabajador

        var employee = await _unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(block.EmployeeId);

        if (employee is null || !employee.IsActive)
            throw new ArgumentException(
                "El trabajador no está disponible.");

        // 3. Verificar otros bloqueos

        var blocks = await _unitOfWork
            .Repository<ScheduleBlockEntity>()
            .FindAsync(b =>
                b.Id != block.Id &&
                b.EmployeeId == block.EmployeeId &&
                b.StartAt < endUtc &&
                b.EndAt > startUtc);

        ScheduleBlockRules.ValidateBlockConflicts(
            startUtc,
            endUtc,
            blocks);

        // 4. Verificar citas existentes

        var appointments = await _unitOfWork
            .Repository<AppointmentEntity>()
            .FindAsync(a =>
                a.EmployeeId == block.EmployeeId &&
                a.StartAt < endUtc &&
                a.EndAt > startUtc);

        ScheduleBlockRules.ValidateAppointmentConflicts(
            startUtc,
            endUtc,
            appointments);

        // 5. Actualizar bloqueo

        block.StartAt = startUtc;
        block.EndAt = endUtc;

        block.Reason =
            string.IsNullOrWhiteSpace(request.Reason)
                ? null
                : request.Reason.Trim();

        _unitOfWork
            .Repository<ScheduleBlockEntity>()
            .Update(block);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Unit.Value;
    }
}