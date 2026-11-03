using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class Diagnosis
{
    public long Id { get; set; }

    public long ConsultationId { get; set; }

    public string Description { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public virtual Consultation Consultation { get; set; } = null!;
}
