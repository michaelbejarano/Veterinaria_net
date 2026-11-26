using vet.Domain.Entities;

namespace vet.Domain.Services.Commission;

public static class CommissionSettlementRules
{
    public static void ValidateCreation(
        long employeeId,
        DateOnly date,
        DateOnly today)
    {
        if (employeeId <= 0)
            throw new ArgumentException(
                "El trabajador es obligatorio.");

        if (date == default)
            throw new ArgumentException(
                "La fecha de liquidación es obligatoria.");

        if (date > today)
            throw new ArgumentException(
                "No se puede liquidar una fecha futura.");
    }

    public static void ValidateCommissions(
        long employeeId,
        IEnumerable<Entities.Commission> commissions)
    {
        var items = commissions.ToList();

        if (!items.Any())
            throw new ArgumentException(
                "No existen comisiones pendientes para liquidar.");

        if (items.Any(c => c.EmployeeId != employeeId))
            throw new ArgumentException(
                "Las comisiones deben pertenecer al mismo trabajador.");

        if (items.Any(c => c.Status != "PENDING"))
            throw new ArgumentException(
                "Todas las comisiones deben estar pendientes.");

        if (items.Any(c => c.Amount < 0))
            throw new ArgumentException(
                "Los importes no pueden ser negativos.");
    }

    public static void ValidatePayment(string status)
    {
        if (status != "PENDING")
            throw new ArgumentException(
                "Solo se pueden pagar liquidaciones pendientes.");
    }

    public static decimal CalculateTotal(
        IEnumerable<Entities.Commission> commissions)
    {
        return commissions.Sum(c => c.Amount);
    }
}