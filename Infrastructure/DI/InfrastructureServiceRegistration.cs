using Application.Abstractions.Common;
using Application.Abstractions.Repositories;
using Ecom.Infrastructure.Messaging;
using Ecom.Infrastructure.Outbox;
using Ecom.Infrastructure.Persistence;
using Infrastructure.Cache;
using Infrastructure.Persistence.DatabaseContext;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Persistence.Repositories.Common;
using Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using StackExchange.Redis;
using System;

namespace Ecom.Infrastructure.DI
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, bool addOutboxPublisher = true)
        {
            services.AddDatabase(config);
            services.AddRepositories();
            services.AddCaching(config);
            services.AddSearch(config);

            // MassTransit publisher (WebApi) registration
            MassTransitConfig.AddMassTransitPublisher(services, config);

            // Register Outbox publisher hosted service optionally
            if (addOutboxPublisher) services.AddHostedService<OutboxPublisherService>();

            return services;
        }

        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<ApplicationDbContext>(opts => opts.UseNpgsql(config.GetConnectionString("DefaultConnection")));
            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(Application.Abstractions.Repositories.Common.IRepository<>), typeof(Repository<>));
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }

        public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(config["Redis:Configuration"] ?? "localhost:6379"));
            services.AddSingleton<RedisCacheService>();
            return services;
        }

        public static IServiceCollection AddSearch(this IServiceCollection services, IConfiguration config)
        {
            var settings = new ConnectionSettings(new Uri(config["Elastic:Url"] ?? "http://elasticsearch:9200")).DefaultIndex("products");
            services.AddSingleton<IElasticClient>(new ElasticClient(settings));
            services.AddSingleton<ElasticService>();
            return services;
        }
    }
}