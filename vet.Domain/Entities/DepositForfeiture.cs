using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class DepositForfeiture
{
    public long Id { get; set; }

    public long PaymentId { get; set; }

    public long AppointmentId { get; set; }

    public decimal Amount { get; set; }

    public string Reason { get; set; } = null!;

    public DateTime Date { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Payment Payment { get; set; } = null!;
}
