using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class ServiceSupply
{
    public long ServiceId { get; set; }

    public long ProductId { get; set; }

    public decimal Quantity { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
