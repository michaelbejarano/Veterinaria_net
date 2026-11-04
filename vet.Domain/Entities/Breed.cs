using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class Breed
{
    public long Id { get; set; }

    public long SpeciesId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();

    public virtual Species Species { get; set; } = null!;
}
