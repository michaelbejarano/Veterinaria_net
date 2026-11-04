using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class CashSession
{
    public long Id { get; set; }

    public long CashRegisterId { get; set; }

    public long OpenedByUserId { get; set; }

    public long? ClosedByUserId { get; set; }

    public DateTime OpenedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public decimal OpeningFloat { get; set; }

    public decimal? CountedCash { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<CashMovement> CashMovements { get; set; } = new List<CashMovement>();

    public virtual CashRegister CashRegister { get; set; } = null!;

    public virtual User? ClosedByUser { get; set; }

    public virtual User OpenedByUser { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
