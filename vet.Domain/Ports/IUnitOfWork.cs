namespace vet.Domain.Ports;

public interface IUnitOfWork
{
    IUserRepository Users { get; }

    IGenericRepository<T> Repository<T>()
        where T : class;

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
    //evitar apgos duplcaido enal creaciond e citas
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
 //blqoueo
    Task<vet.Domain.Entities.Appointment?> GetAppointmentForUpdateAsync(
        long appointmentId,
        CancellationToken cancellationToken = default);
    //cashs esion
    Task<vet.Domain.Entities.CashSession?> GetCashSessionForUpdateAsync(
        long cashSessionId,
        CancellationToken cancellationToken = default);
}