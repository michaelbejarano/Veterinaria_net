using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class CashRegister
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<CashSession> CashSessions
    { get; set; } = new List<CashSession>();

    public virtual Company Company { get; set; } = null!;
}
