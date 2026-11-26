using vet.Domain.Entities;

namespace vet.Application.UseCases.Report.Queries;

public static class ReportHelper
{
    private static readonly TimeZoneInfo PeruTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Lima");

    public static DateOnly Today()
    {
        return DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow, PeruTimeZone));
    }

    public static DateOnly LocalDate(DateTime date)
    {
        return DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(
                date, PeruTimeZone));
    }

    public static (DateTime Start, DateTime End) GetRange(
        DateOnly fromDate,
        DateOnly toDate)
    {
        if (fromDate > toDate)
            throw new ArgumentException(
                "El rango de fechas es inválido.");

        if (toDate == DateOnly.MaxValue)
            throw new ArgumentException(
                "La fecha final es inválida.");

        if (toDate.DayNumber - fromDate.DayNumber > 365)
            throw new ArgumentException(
                "El periodo máximo es de 366 días.");

        var start = TimeZoneInfo.ConvertTimeToUtc(
            fromDate.ToDateTime(TimeOnly.MinValue),
            PeruTimeZone);

        var end = TimeZoneInfo.ConvertTimeToUtc(
            toDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
            PeruTimeZone);

        return (start, end);
    }

    public static decimal ItemTotal(SaleItem item)
    {
        return decimal.Round(
            item.Quantity * item.UnitPrice
            - item.Discount + item.Igv,
            2,
            MidpointRounding.AwayFromZero);
    }
}