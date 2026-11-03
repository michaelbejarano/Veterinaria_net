using System;
using System.Collections.Generic;

namespace vet.Infrastructure.Persistence.Models;

public partial class Company
{
    public long Id { get; set; }

    public string Ruc { get; set; } = null!;

    public string LegalName { get; set; } = null!;

    public string? TradeName { get; set; }

    public string Address { get; set; } = null!;

    public string? Ubigeo { get; set; }

    public string? Department { get; set; }

    public string? Province { get; set; }

    public string? District { get; set; }

    public string? Urbanization { get; set; }

    public string? LocalCode { get; set; }

    public string? SolUser { get; set; }

    public string? SolSecretRef { get; set; }

    public string? CertificatePath { get; set; }

    public string? CertificadoSecretRef { get; set; }

    public string Environment { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CashRegister> CashRegisters { get; set; } = new List<CashRegister>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
