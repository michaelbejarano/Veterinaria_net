using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class ScheduleBlock
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string? Reason { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
