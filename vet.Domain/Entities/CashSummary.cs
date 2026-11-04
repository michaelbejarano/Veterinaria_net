using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class CashSummary
{
    public long? SessionId { get; set; }

    public long? CashRegisterId { get; set; }

    public DateTime? OpenedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string? Status { get; set; }

    public decimal? OpeningFloat { get; set; }

    public decimal? CashInflows { get; set; }

    public decimal? CashOutflows { get; set; }

    public decimal? ExpectedCash { get; set; }

    public decimal? CountedCash { get; set; }
}
