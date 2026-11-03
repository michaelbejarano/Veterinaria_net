using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class InventoryMovement
{
    public long Id { get; set; }

    public long BatchId { get; set; }

    public string Type { get; set; } = null!;

    public decimal Quantity { get; set; }

    public long? VisitServiceId { get; set; }

    public long? SaleItemId { get; set; }

    public string? Reference { get; set; }

    public decimal UnitCost { get; set; }

    public DateTime Date { get; set; }

    public virtual ProductBatch Batch { get; set; } = null!;

    public virtual SaleItem? SaleItem { get; set; }

    public virtual VisitService? VisitService { get; set; }
}
