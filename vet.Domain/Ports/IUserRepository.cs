using vet.Domain.Entities;

namespace vet.Domain.Ports;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);

    Task<bool> ExistsByEmailAsync(string email);

    Task<IEnumerable<User>> GetActiveUsersAsync();
}