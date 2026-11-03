using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class SettlementItem
{
    public long SettlementId { get; set; }

    public long CommissionId { get; set; }

    public virtual Commission Commission { get; set; } = null!;

    public virtual CommissionSettlement Settlement { get; set; } = null!;
}
