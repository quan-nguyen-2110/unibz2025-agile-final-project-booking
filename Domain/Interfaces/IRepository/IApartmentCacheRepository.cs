using Domain.Entities;

namespace Domain.Interfaces.IRepository
{
    public interface IApartmentCacheRepository
    {
        Task<ApartmentCache> AddAsync(ApartmentCache apartment, CancellationToken ct = default);
        Task<int> UpdateAsync(ApartmentCache apartment, CancellationToken ct = default);
        Task<bool> IsExistingAsync(Guid id, CancellationToken ct = default);
        Task DeleteAsync(Guid id);
        Task<ApartmentCache?> GetByIdAsync(Guid id, CancellationToken ct = default);
    }
}
