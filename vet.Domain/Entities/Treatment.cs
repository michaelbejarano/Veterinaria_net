using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class Treatment
{
    public long Id { get; set; }

    public long ConsultationId { get; set; }

    public string Description { get; set; } = null!;

    public string? Dosage { get; set; }

    public string? Frequency { get; set; }

    public string? Duration { get; set; }

    public string? Instructions { get; set; }

    public virtual Consultation Consultation { get; set; } = null!;
}
