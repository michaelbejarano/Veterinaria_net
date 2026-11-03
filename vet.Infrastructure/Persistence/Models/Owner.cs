using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class Owner
{
    public long Id { get; set; }

    public string? DocumentType { get; set; }

    public string? DocumentNumber { get; set; }

    public string FirstNames { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
