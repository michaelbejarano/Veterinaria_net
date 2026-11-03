using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class Appointment
{
    public long Id { get; set; }

    public long PetId { get; set; }

    public long EmployeeId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string Status { get; set; } = null!;

    public decimal ReservedPrice { get; set; }

    public decimal DepositPercent { get; set; }

    public decimal RequiredDeposit { get; set; }

    public DateTime? PaymentExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AppointmentReschedule? AppointmentReschedule { get; set; }

    public virtual ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();

    public virtual ICollection<DepositForfeiture> DepositForfeitures { get; set; } = new List<DepositForfeiture>();

    public virtual Employee Employee { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Pet Pet { get; set; } = null!;

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    public virtual Visit? Visit { get; set; }
}
