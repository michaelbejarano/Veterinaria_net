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
}