using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class CommissionRule
{
    public long Id { get; set; }

    public long EmployeeServiceId { get; set; }

    public string Type { get; set; } = null!;

    public decimal Value { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public virtual ICollection<Commission> Commissions { get; set; } = new List<Commission>();

    public virtual EmployeeService EmployeeService { get; set; } = null!;
}
