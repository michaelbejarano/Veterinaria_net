namespace vet.Domain.DTOs.Employee;

public class CreateEmployeeDto
{
    public long? UserId { get; set; }

    public string FirstNames { get; set; } = string.Empty;
    public string LastNames { get; set; } = string.Empty;

    public string? DocumentNumber { get; set; }
    public string? Phone { get; set; }

    public string Type { get; set; } = string.Empty;

    public string? LicenseNumber { get; set; }
    public string? Specialty { get; set; }

    public decimal MonthlySalary { get; set; }
}