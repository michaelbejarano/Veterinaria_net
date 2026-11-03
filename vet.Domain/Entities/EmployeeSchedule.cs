using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class EmployeeSchedule
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }

    public short Weekday { get; set; }

    public TimeOnly StartAt { get; set; }

    public TimeOnly EndAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
