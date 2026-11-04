using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class EmployeeService
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }

    public long ServiceId { get; set; }

    public bool IsEnabled { get; set; }

    public virtual ICollection<CommissionRule> CommissionRules { get; set; } = new List<CommissionRule>();

    public virtual Employee Employee { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
