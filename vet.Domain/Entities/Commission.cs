using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class Commission
{
    public long Id { get; set; }

    public long VisitServiceId { get; set; }

    public long EmployeeId { get; set; }

    public long? RuleId { get; set; }

    public string AppliedType { get; set; } = null!;

    public decimal AppliedValue { get; set; }

    public decimal CalculationBase { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime Date { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual CommissionRule? Rule { get; set; }

    public virtual SettlementItem? SettlementItem { get; set; }

    public virtual VisitService VisitService { get; set; } = null!;
}
