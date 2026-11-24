
using MediatR;
using vet.Domain.Ports;
using vet.Domain.Entities;

namespace vet.Application.UseCases.Appointment.Queries;

public class GetAllAppointmentsQuery : IRequest<List<AppointmentListItem>>
{
}

public class AppointmentListItem
{
    public long Id { get; set; }
    public long PetId { get; set; }
    public string PetName { get; set; } = string.Empty;
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ReservedPrice { get; set; }
    public decimal RequiredDeposit { get; set; }
}

public class GetAllAppointmentsQueryHandler
    : IRequestHandler<GetAllAppointmentsQuery, List<AppointmentListItem>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllAppointmentsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AppointmentListItem>> Handle(
        GetAllAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var appointments = await _unitOfWork
            .Repository<Domain.Entities.Appointment>()
            .GetAllAsync();

        var pets = (await _unitOfWork
            .Repository<Domain.Entities.Pet>()
            .GetAllAsync()).ToDictionary(p => p.Id);

        var employees = (await _unitOfWork
            .Repository<Domain.Entities.Employee>()
            .GetAllAsync()).ToDictionary(e => e.Id);

        return appointments
            .OrderBy(a => a.StartAt)
            .Select(a =>
            {
                pets.TryGetValue(a.PetId, out var pet);
                employees.TryGetValue(a.EmployeeId, out var employee);

                return new AppointmentListItem
                {
                    Id = a.Id,
                    PetId = a.PetId,
                    PetName = pet?.Name ?? "Sin mascota",
                    EmployeeId = a.EmployeeId,
                    EmployeeName = employee is null
                        ? "Sin trabajador"
                        : $"{employee.FirstNames} {employee.LastNames}",
                    StartAt = a.StartAt,
                    EndAt = a.EndAt,
                    Status = a.Status,
                    ReservedPrice = a.ReservedPrice,
                    RequiredDeposit = a.RequiredDeposit
                };
            })
            .ToList();
    }
}
