using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class Consultation
{
    public long Id { get; set; }

    public long VisitId { get; set; }

    public long VeterinarianId { get; set; }

    public DateTime Date { get; set; }

    public decimal? WeightKg { get; set; }

    public string? Reason { get; set; }

    public string? Anamnesis { get; set; }

    public string? PhysicalExam { get; set; }

    public string? Instructions { get; set; }

    public virtual Diagnosis? Diagnosis { get; set; }

    public virtual ICollection<Treatment> Treatments { get; set; } = new List<Treatment>();

    public virtual Employee Veterinarian { get; set; } = null!;

    public virtual Visit Visit { get; set; } = null!;
}
