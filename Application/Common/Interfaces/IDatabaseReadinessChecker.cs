using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IDatabaseReadinessChecker
    {
        Task<bool> IsDatabaseReadyAsync(CancellationToken ct);
        Task<bool> IsApartmentCacheReadyAsync(CancellationToken ct);
        Task SynchronizedApartmentCachesAsync(List<ApartmentCache> apts, CancellationToken ct);
    }
}
