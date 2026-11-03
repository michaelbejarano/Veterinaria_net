using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class AppointmentService
{
    public long Id { get; set; }

    public long AppointmentId { get; set; }

    public long ServiceId { get; set; }

    public decimal AgreedPrice { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
