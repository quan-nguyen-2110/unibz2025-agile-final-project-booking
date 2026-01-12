using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence
{
    public class DatabaseChecker : IDatabaseReadinessChecker
    {
        private readonly ILogger<DatabaseChecker> _logger;
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        public DatabaseChecker(ILogger<DatabaseChecker> logger, IDbContextFactory<AppDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public async Task<bool> IsDatabaseReadyAsync(CancellationToken ct)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(ct);
                return await context.Database.CanConnectAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Database not ready yet: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> IsApartmentCacheReadyAsync(CancellationToken ct)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(ct);
                return await context.ApartmentCaches.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Database not ready yet: {Message}", ex.Message);
                return false;
            }
        }

        public async Task SynchronizedApartmentCachesAsync(List<ApartmentCache> apts, CancellationToken ct)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(ct);
                await context.ApartmentCaches.AddRangeAsync(apts, ct);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Database not ready yet: {Message}", ex.Message);
            }
        }

        public async Task<bool> IsUserCacheReadyAsync(CancellationToken ct)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(ct);
                return await context.UserCaches.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Database not ready yet: {Message}", ex.Message);
                return false;
            }
        }

        public async Task SynchronizedUserCachesAsync(List<UserCache> users, CancellationToken ct)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(ct);
                await context.UserCaches.AddRangeAsync(users, ct);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Database not ready yet: {Message}", ex.Message);
            }
        }
    }
}
