using Application.Common.Interfaces;
using Application.Common.Interfaces.IMessaging;
using Domain.Interfaces.IRepository;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            //services.AddDbContext<AppDbContext>(options =>
            //    options.UseSqlServer(config.GetConnectionString("Default")));

            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("Default")));

            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IApartmentCacheRepository, ApartmentCacheRepository>();
            services.AddScoped<IUserCacheRepository, UserCacheRepository>();

            services.AddSingleton<IDatabaseReadinessChecker, DatabaseChecker>();

            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

            services.AddHostedService<RabbitMqConsumer>();

            return services;
        }
    }
}
