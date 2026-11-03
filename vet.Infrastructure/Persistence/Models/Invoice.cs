using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class Invoice
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long? SaleId { get; set; }

    public string Type { get; set; } = null!;

    public string Series { get; set; } = null!;

    public long SequenceNumber { get; set; }

    public string? CustomerDocType { get; set; }

    public string? CustomerDocNumber { get; set; }

    public string CustomerName { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public decimal Igv { get; set; }

    public decimal Total { get; set; }

    public string Status { get; set; } = null!;

    public string? SunatCode { get; set; }

    public string? SunatMessage { get; set; }

    public string? XmlRef { get; set; }

    public string? CdrRef { get; set; }

    public DateTime Date { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual Sale? Sale { get; set; }
}
