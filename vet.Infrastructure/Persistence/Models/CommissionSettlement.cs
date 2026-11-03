using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class CommissionSettlement
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }

    public DateOnly Date { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual ICollection<SettlementItem> SettlementItems { get; set; } = new List<SettlementItem>();
}
