using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class Product
{
    public long Id { get; set; }

    public string? Sku { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal SalePrice { get; set; }

    public bool TrackBatches { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<ProductBatch> ProductBatches { get; set; } = new List<ProductBatch>();

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    public virtual ICollection<ServiceSupply> ServiceSupplies { get; set; } = new List<ServiceSupply>();
}
