using System;
using System.Collections.Generic;

namespace vet.Domain.Entities;

public partial class DepositApplication
{
    public long Id { get; set; }

    public long PaymentId { get; set; }

    public long SaleId { get; set; }

    public decimal Amount { get; set; }

    public virtual Payment Payment { get; set; } = null!;

    public virtual Sale Sale { get; set; } = null!;
}
