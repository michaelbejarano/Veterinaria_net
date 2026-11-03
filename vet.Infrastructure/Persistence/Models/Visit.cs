using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class Visit
{
    public long Id { get; set; }

    public long PetId { get; set; }

    public long? AppointmentId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Consultation? Consultation { get; set; }

    public virtual Pet Pet { get; set; } = null!;

    public virtual ICollection<VisitService> VisitServices { get; set; } = new List<VisitService>();
}
