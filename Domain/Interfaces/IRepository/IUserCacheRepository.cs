using Domain.Entities;

namespace Domain.Interfaces.IRepository
{
    public interface IUserCacheRepository
    {
        Task<UserCache> AddAsync(UserCache user, CancellationToken ct = default);
        Task<int> UpdateAsync(UserCache user, CancellationToken ct = default);
        Task<bool> IsExistingAsync(Guid id, CancellationToken ct = default);
        Task DeleteAsync(Guid id);
    }
}
