using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class VisitService
{
    public long Id { get; set; }

    public long VisitId { get; set; }

    public long ServiceId { get; set; }

    public long EmployeeId { get; set; }

    public decimal FinalPrice { get; set; }

    public string Status { get; set; } = null!;

    public virtual Commission? Commission { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();

    public virtual SaleItem? SaleItem { get; set; }

    public virtual Service Service { get; set; } = null!;

    public virtual Visit Visit { get; set; } = null!;
}
