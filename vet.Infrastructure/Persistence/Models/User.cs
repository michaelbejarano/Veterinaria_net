using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class User
{
    public long Id { get; set; }

    public long RoleId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CashSession> CashSessionClosedByUsers { get; set; } = new List<CashSession>();

    public virtual ICollection<CashSession> CashSessionOpenedByUsers { get; set; } = new List<CashSession>();

    public virtual Employee? Employee { get; set; }

    public virtual Role Role { get; set; } = null!;
}
