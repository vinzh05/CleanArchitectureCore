using Application.Abstractions;
using Application.Abstractions.Common;
using Ecom.Infrastructure.Persistence;
using Infrastructure.Cache;
using Infrastructure.Messaging;
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.DI
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddDatabase(config)
                    .AddRepositories(config)
                    .AddCaching(config)
                    .AddSearch(config)
                    .AddMessaging(config);

            return services;
        }

        private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseNpgsql(config.GetConnectionString("DefaultConnection")));

            return services;
        }

        private static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped(typeof(Application.Abstractions.Common.IRepository<>), typeof(Repository<>));
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }

        private static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(config["Redis:Configuration"] ?? "localhost:6379"));
            services.AddSingleton<IRedisCacheService, RedisCacheService>();
            return services;
        }

        private static IServiceCollection AddSearch(this IServiceCollection services, IConfiguration config)
        {
            var settings = new ConnectionSettings(new Uri(config["Elastic:Url"] ?? "http://elasticsearch:9200")).DefaultIndex("products");
            services.AddSingleton<IElasticClient>(new ElasticClient(settings));
            services.AddSingleton<IElasticService, ElasticService>();
            return services;
        }

        private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration config)
        {
            services.AddMassTransitWithRabbitMq(config);
            return services;
        }
    }
}
