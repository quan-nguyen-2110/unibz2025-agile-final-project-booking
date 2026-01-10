
using Application.Common.Interfaces;
using Azure;
using Domain.Entities;

namespace Api.HostedServices
{
    public class StartupInitializer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IDatabaseReadinessChecker _dbChecker;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StartupInitializer> _logger;

        public StartupInitializer(
            IConfiguration configuration,
            IDatabaseReadinessChecker dbChecker,
            IHttpClientFactory httpClientFactory,
            ILogger<StartupInitializer> logger)
        {
            _configuration = configuration;
            _dbChecker = dbChecker;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Waiting for database to be ready...");

            const int delaySeconds = 5;
            int retries = 0;
            const int maxRetries = 30;

            while (retries < maxRetries && !stoppingToken.IsCancellationRequested)
            {
                if (await _dbChecker.IsDatabaseReadyAsync(stoppingToken))
                {
                    _logger.LogInformation("Database is ready");
                    break;
                }

                retries++;
                _logger.LogInformation("Retrying in {Delay}s...", delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }

            if (retries == maxRetries)
            {
                throw new Exception("Database did not become ready in time");
            }

            // 👉 Call external API AFTER DB is ready
            await CallStartupApiAsync(stoppingToken);
        }

        private async Task CallStartupApiAsync(CancellationToken ct)
        {
            _logger.LogInformation("Checking ApartmentCache data ....");
            if (!(await _dbChecker.IsApartmentCacheReadyAsync(ct)))
            {
                _logger.LogInformation("Calling startup API...");
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(_configuration["SyncApartmentsURL"], ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Startup API call succeeded");
                    var content = await response.Content.ReadFromJsonAsync<List<ApartmentCache>>(ct);
                    await _dbChecker.SynchronizedApartmentCachesAsync(content!, ct);
                }
                else
                {
                    _logger.LogError("Startup API failed: {StatusCode}", response.StatusCode);
                }
            }
            else
            {
                _logger.LogInformation("ApartmentCache has been synchronized already.");
            }
        }
    }
}
