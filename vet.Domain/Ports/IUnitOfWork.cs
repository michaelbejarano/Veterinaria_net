using vet.Domain.Entities;

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
    
    Task<ProductBatch?> GetProductBatchForUpdateAsync(
        long batchId,
        CancellationToken cancellationToken = default);

    Task<long> GetNextInternalReceiptNumberAsync(
        long companyId,
        string series,
        CancellationToken cancellationToken = default);
    
    Task<Sale?> GetSaleForUpdateAsync(
        long saleId,
        CancellationToken cancellationToken = default);
    
    Task<AppointmentReschedule?> GetRescheduleForUpdateAsync(
        long rescheduleId,
        CancellationToken cancellationToken = default);
    Task<Employee?> GetEmployeeForUpdateAsync(
        long employeeId,
        CancellationToken cancellationToken = default);

    Task<CommissionSettlement?> GetCommissionSettlementForUpdateAsync(
        long settlementId,
        CancellationToken cancellationToken = default);
    
}