using vet.Domain.Ports;
using EmployeeEntity = vet.Domain.Entities.Employee;
using ScheduleEntity = vet.Domain.Entities.EmployeeSchedule;

namespace vet.Application.Validators.EmployeeSchedule;

public class EmployeeScheduleValidator(
    IUnitOfWork unitOfWork)
{
    public async Task ValidateAsync(
        long employeeId,
        short weekday,
        TimeOnly startAt,
        TimeOnly endAt,
        long? currentScheduleId = null)
    {
        // 1. Verificar que exista el empleado
        var employee = await unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(employeeId);

        if (employee is null)
            throw new ArgumentException(
                "El empleado no existe.");

        // 2. Verificar que esté activo
        if (!employee.IsActive)
            throw new InvalidOperationException(
                "No se pueden asignar horarios a empleados inactivos.");

        // 3. Validar día de la semana
        // Convención: 1 = lunes, ..., 7 = domingo
        if (weekday < 1 || weekday > 7)
            throw new ArgumentException(
                "El día debe estar entre 1 (lunes) y 7 (domingo).");

        // 4. Validar rango de horas
        if (startAt >= endAt)
            throw new ArgumentException(
                "La hora de inicio debe ser menor que la hora de fin.");

        // 5. Buscar horarios superpuestos
        var schedules = await unitOfWork
            .Repository<ScheduleEntity>()
            .FindAsync(s =>
                s.EmployeeId == employeeId &&
                s.Weekday == weekday &&
                (!currentScheduleId.HasValue ||
                 s.Id != currentScheduleId.Value));

        var hasOverlap = schedules.Any(s =>
            startAt < s.EndAt &&
            endAt > s.StartAt);

        if (hasOverlap)
            throw new InvalidOperationException(
                "El empleado ya tiene un horario que se superpone.");
    }
}