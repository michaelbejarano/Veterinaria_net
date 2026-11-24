using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using vet.Domain.Entities;
using vet.Domain.Ports;

namespace vet.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly VetCareContext _context;

    public IUserRepository Users { get; }

    public UnitOfWork(VetCareContext context)
    {
        _context = context;

        Users = new UserRepository(context);
    }

    public IGenericRepository<T> Repository<T>()
        where T : class
    {
        return new GenericRepository<T>(_context);
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(
            cancellationToken);
    }
    
    //evitar pago duplciados en la creaciond ecitas
    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    public async Task<Appointment?> GetAppointmentForUpdateAsync(
        long appointmentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Appointment>()
            .FromSqlInterpolated(
                $"""
                 SELECT *
                 FROM vetcare.appointments
                 WHERE id = {appointmentId}
                 FOR UPDATE
                 """)
            .SingleOrDefaultAsync(cancellationToken);
    }
    
    //cash sesion
    public async Task<vet.Domain.Entities.CashSession?>
        GetCashSessionForUpdateAsync(
            long cashSessionId,
            CancellationToken cancellationToken = default)
    {
        return await _context.Set<vet.Domain.Entities.CashSession>()
            .FromSqlInterpolated(
                $"""
                 SELECT *
                 FROM vetcare.cash_sessions
                 WHERE id = {cashSessionId}
                 FOR UPDATE
                 """)
            .SingleOrDefaultAsync(cancellationToken);
    }
    
    public async Task<ProductBatch?> GetProductBatchForUpdateAsync(
        long batchId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductBatch>()
            .FromSqlInterpolated(
                $"SELECT * FROM vetcare.product_batches WHERE id = {batchId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }
    public async Task<long> GetNextInternalReceiptNumberAsync(
        long companyId,
        string series,
        CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
            throw new ArgumentException(
                "La empresa es inválida.");

        if (string.IsNullOrWhiteSpace(series))
            throw new ArgumentException(
                "La serie del recibo es obligatoria.");

        if (_context.Database.CurrentTransaction == null)
            throw new InvalidOperationException(
                "El correlativo debe generarse dentro de una transacción.");

        // Bloqueo exclusivo para la empresa y serie.
        // Se libera automáticamente al finalizar la transacción.
        var connection = _context.Database.GetDbConnection();

        await using (var command = connection.CreateCommand())
        {
            command.Transaction =
                _context.Database.CurrentTransaction.GetDbTransaction();

            command.CommandText = """
                                  SELECT pg_advisory_xact_lock(
                                      hashtextextended(@series, @companyId)
                                  )
                                  """;

            var seriesParameter = command.CreateParameter();
            seriesParameter.ParameterName = "series";
            seriesParameter.Value = "INTERNAL:" + series;
            command.Parameters.Add(seriesParameter);

            var companyParameter = command.CreateParameter();
            companyParameter.ParameterName = "companyId";
            companyParameter.Value = companyId;
            command.Parameters.Add(companyParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // Después de adquirir el bloqueo,
        // consultamos el último correlativo.
        var lastNumber = await _context.Set<Invoice>()
            .Where(i =>
                i.CompanyId == companyId &&
                i.Type == "INTERNAL" &&
                i.Series == series)
            .MaxAsync(
                i => (long?)i.SequenceNumber,
                cancellationToken);

        return (lastNumber ?? 0) + 1;
    }
    public async Task<Sale?> GetSaleForUpdateAsync(
        long saleId,
        CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction == null)
        {
            throw new InvalidOperationException(
                "El bloqueo de la venta requiere una transacción activa.");
        }

        return await _context.Set<Sale>()
            .FromSqlInterpolated(
                $"SELECT * FROM vetcare.sales WHERE id = {saleId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }
}