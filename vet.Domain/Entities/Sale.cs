using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class Sale
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long OwnerId { get; set; }

    public long? AppointmentId { get; set; }

    public DateTime Date { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<DepositApplication> DepositApplications { get; set; } = new List<DepositApplication>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual Owner Owner { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
