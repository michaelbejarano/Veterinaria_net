using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class CashMovement
{
    public long Id { get; set; }

    public long CashSessionId { get; set; }

    public string Type { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string Method { get; set; } = null!;

    public decimal Amount { get; set; }

    public long? PaymentId { get; set; }

    public string? Reference { get; set; }

    public string? Description { get; set; }

    public DateTime Date { get; set; }

    public virtual CashSession CashSession { get; set; } = null!;

    public virtual Payment? Payment { get; set; }
}
