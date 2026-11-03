using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class Employee
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string FirstNames { get; set; } = null!;

    public string LastNames { get; set; } = null!;

    public string? DocumentNumber { get; set; }

    public string? Phone { get; set; }

    public string Type { get; set; } = null!;

    public string? LicenseNumber { get; set; }

    public string? Specialty { get; set; }

    public decimal MonthlySalary { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<CommissionSettlement> CommissionSettlements { get; set; } = new List<CommissionSettlement>();

    public virtual ICollection<Commission> Commissions { get; set; } = new List<Commission>();

    public virtual ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();

    public virtual ICollection<EmployeeSchedule> EmployeeSchedules { get; set; } = new List<EmployeeSchedule>();

    public virtual ICollection<EmployeeService> EmployeeServices { get; set; } = new List<EmployeeService>();

    public virtual ICollection<SalaryPayment> SalaryPayments { get; set; } = new List<SalaryPayment>();

    public virtual ICollection<ScheduleBlock> ScheduleBlocks { get; set; } = new List<ScheduleBlock>();

    public virtual User? User { get; set; }

    public virtual ICollection<VisitService> VisitServices { get; set; } = new List<VisitService>();
}
