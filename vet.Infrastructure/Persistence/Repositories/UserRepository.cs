using Microsoft.EntityFrameworkCore;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Infrastructure.Persistence;

namespace vet.Infrastructure.Persistence.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(VetCareContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _dbSet
            .AnyAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(u => u.IsActive)
            .ToListAsync();
    }
}