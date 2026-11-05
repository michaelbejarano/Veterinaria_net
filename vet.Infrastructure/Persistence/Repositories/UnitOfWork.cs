using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using vet.Domain.Ports;
using vet.Infrastructure.Persistence;

namespace vet.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly VetCareContext _context;
    private readonly IServiceProvider _serviceProvider;

    public IUserRepository Users { get; }

    public UnitOfWork(
        VetCareContext context,
        IUserRepository userRepository,
        IServiceProvider serviceProvider)
    {
        _context = context;
        Users = userRepository;
        _serviceProvider = serviceProvider;
    }

    public IGenericRepository<T> Repository<T>() where T : class
    {
        return _serviceProvider.GetRequiredService<IGenericRepository<T>>();
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}