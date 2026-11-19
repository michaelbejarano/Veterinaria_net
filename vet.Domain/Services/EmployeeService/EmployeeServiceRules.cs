using vet.Domain.Entities;

namespace vet.Domain.Services;

public static class EmployeeServiceRules
{
    public static void ValidateAssignment(
        Employee employee,
        Service service)
    {
        if (service.IsClinical &&
            employee.Type != "VETERINARIAN")
        {
            throw new ArgumentException(
                "Los servicios clínicos solo pueden " +
                "asignarse a veterinarios autorizados.");
        }
    }
}