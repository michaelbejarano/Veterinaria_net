using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class SaleItem
{
    public long Id { get; set; }

    public long SaleId { get; set; }

    public long? ProductId { get; set; }

    public long? VisitServiceId { get; set; }

    public string Description { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }

    public decimal Igv { get; set; }

    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();

    public virtual Product? Product { get; set; }

    public virtual Sale Sale { get; set; } = null!;

    public virtual VisitService? VisitService { get; set; }
}
