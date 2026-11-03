using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class AppointmentReschedule
{
    public long Id { get; set; }

    public long AppointmentId { get; set; }

    public DateTime PreviousStart { get; set; }

    public DateTime PreviousEnd { get; set; }

    public DateTime NewStart { get; set; }

    public DateTime NewEnd { get; set; }

    public decimal Fee { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PaymentExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
