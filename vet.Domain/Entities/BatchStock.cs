using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class BatchStock
{
    public long? BatchId { get; set; }

    public long? ProductId { get; set; }

    public string? BatchCode { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public decimal? Stock { get; set; }
}
