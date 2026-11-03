using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class ProductBatch
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public string BatchCode { get; set; } = null!;

    public DateOnly? ExpiryDate { get; set; }

    public decimal UnitCost { get; set; }

    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();

    public virtual Product Product { get; set; } = null!;
}
