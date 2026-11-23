using Microsoft.EntityFrameworkCore;
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
}