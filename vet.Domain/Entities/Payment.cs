using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class Payment
{
    public long Id { get; set; }

    public long? SaleId { get; set; }

    public long? AppointmentId { get; set; }

    public long? RescheduleId { get; set; }

    public long CashSessionId { get; set; }

    public string Method { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? Reference { get; set; }

    public DateTime Date { get; set; }

    public string Status { get; set; } = null!;

    public virtual Appointment? Appointment { get; set; }

    public virtual CashMovement? CashMovement { get; set; }

    public virtual CashSession CashSession { get; set; } = null!;

    public virtual ICollection<DepositApplication> DepositApplications { get; set; } = new List<DepositApplication>();

    public virtual DepositForfeiture? DepositForfeiture { get; set; }

    public virtual AppointmentReschedule? Reschedule { get; set; }

    public virtual Sale? Sale { get; set; }
}
