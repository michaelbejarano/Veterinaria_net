namespace vet.Application.DTOs.EmployeeSchedule;

public class EmployeeScheduleDto
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public short Weekday { get; set; }
    public TimeOnly StartAt { get; set; }
    public TimeOnly EndAt { get; set; }
}