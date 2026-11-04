using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class Species
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Breed> Breeds { get; set; } = new List<Breed>();

    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
}
