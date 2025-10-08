using Application.Abstractions.Common;
using Application.Abstractions.Infrastructure;
using Application.Abstractions.Repositories;
using Application.Abstractions.SignalR;
using Ecom.Infrastructure.Persistence;
using Infrastructure.Cache;
using Infrastructure.Messaging.Extensions;
using Infrastructure.Middlewares;
using Infrastructure.Persistence.DatabaseContext;
using Infrastructure.Persistence.JWT;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Persistence.Repositories.Common;
using Infrastructure.Search;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Shared.IntegrationEvents.Contracts;
using StackExchange.Redis;
using System;
using System.Text;
using System.Threading.RateLimiting;

namespace Infrastructure.DI
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration config,
            bool addOutboxPublisher = false)
        {
            // Database + Repositories + Caching + JWT
            services.AddDatabase(config)
                    .AddSearch(config)
                    .AddCaching(config)
                    .AddJwtAuthentication(config)
                    .AddRepositories()
                    .AddMiddlewares();

            // MediatR registration
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(InfrastructureServiceRegistration).Assembly));

            // RabbitMQ Publisher registration (for WebApi)
            services.AddRabbitMqPublisher(config);

            // Register integration event types for outbox deserialization
            services.RegisterIntegrationEvents(
                typeof(Shared.IntegrationEvents.Contracts.Product.ProductCreatedIntegrationEvent).Assembly,
                type => type.Name.EndsWith("IntegrationEvent"));

            // Register Outbox publisher hosted service if needed
            if (addOutboxPublisher)
            {
                services.AddOutboxPublisher(config, registry =>
                {
                    // Additional type registrations can be done here
                });
            }

            return services;
        }

        #region Database
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<ApplicationDbContext>(opts => opts.UseNpgsql(config.GetConnectionString("DefaultConnection")));
            return services;
        }

        #endregion

        #region Repositories
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }
        #endregion

        #region Caching
        public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration config)
        {
            var redisConfig = config.GetConnectionString("Redis") ?? "localhost:6379";
            services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConfig));
            services.AddSingleton<IRedisCacheService, RedisCacheService>();
            return services;
        }
        #endregion

        #region Search
        public static IServiceCollection AddSearch(this IServiceCollection services, IConfiguration config)
        {
            var settings = new ConnectionSettings(new Uri(config["Elastic:Url"] ?? "http://elasticsearch:9200")).DefaultIndex("products");
            services.AddSingleton<IElasticClient>(new ElasticClient(settings));
            services.AddSingleton<ElasticService>();
            return services;
        }
        #endregion

        #region JWT Authentication
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            var jwtSettings = config.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new Exception("JWT key missing"));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddScoped<IJwtProvider, JwtProvider>();

            return services;
        }
        #endregion

        #region Middlewares & Other Services
        public static IServiceCollection AddMiddlewares(this IServiceCollection services)
        {
            services.AddScoped<ExceptionHandlingMiddleware>();
            services.AddScoped<ValidationMiddleware>();
            services.AddScoped<RequestLoggingMiddleware>();
            services.AddScoped<PerformanceMiddleware>();
            services.AddScoped<CorrelationIdMiddleware>();
            services.AddScoped<SecurityHeadersMiddleware>();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
            });

            services.AddResponseCompression();
            services.AddResponseCaching();
            services.AddHealthChecks();

            return services;
        }
        #endregion

        #region Setup Rate Limiting (Placeholder)
        public static IServiceCollection RateLimit(this IServiceCollection services, IConfiguration config)
        {
            services.AddRateLimiter(options =>
            {
                options.AddPolicy("PerIpPolicy", context =>
                {
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ip,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });
            });

            return services;
        }
        #endregion
    }
}