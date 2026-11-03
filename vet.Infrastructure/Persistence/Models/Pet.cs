using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class Pet
{
    public long Id { get; set; }

    public long OwnerId { get; set; }

    public long SpeciesId { get; set; }

    public long? BreedId { get; set; }

    public string Name { get; set; } = null!;

    public char? Sexo { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual Breed? Breed { get; set; }

    public virtual Owner Owner { get; set; } = null!;

    public virtual Species Species { get; set; } = null!;

    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
