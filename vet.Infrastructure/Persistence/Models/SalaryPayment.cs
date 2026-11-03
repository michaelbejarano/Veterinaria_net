using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class SalaryPayment
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }

    public DateOnly Period { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaidAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;
}
