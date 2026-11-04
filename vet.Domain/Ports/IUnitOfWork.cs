namespace vet.Domain.Ports;

public interface IUnitOfWork
{
    IUserRepository Users { get; }

    IGenericRepository<T> Repository<T>()
        where T : class;

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}