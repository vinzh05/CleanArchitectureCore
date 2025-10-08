using Application.Abstractions.SignalR;
using CleanArchitectureCore.Controllers;
using Infrastructure.DI;
using Infrastructure.Middlewares;
using Infrastructure.Search;
using Infrastructure.SignalR;
using System.Reflection;

namespace CleanArchitectureCore
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Cấu hình
            builder.Configuration.AddDefaultConfiguration();

            // Đăng ký dịch vụ cơ bản
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Đăng ký hạ tầng + application
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplicationServices();
            builder.Services.AddHttpContextAccessor();

            // Đăng ký BackgroundServices
            builder.Services.AddHostedService<BackgroundServices.PaymentExpiryCheckerService>();
            builder.Services.AddHostedService<BackgroundServices.OutboxPublisherService>();

            //SignalR
            builder.Services.AddSignalR();
            builder.Services.AddScoped<INotificationHubContext, NotificationHub>();
            builder.Services.AddScoped<INotificationHub, NotificationHubAdapter>();

            var app = builder.Build();

            // Swagger (chỉ bật khi dev/staging)
            if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //Middleware
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMiddleware<ValidationMiddleware>();
            app.UseMiddleware<PerformanceMiddleware>();
            app.UseMiddleware<SecurityHeadersMiddleware>();

            app.UseCors("AllowAll");
            app.UseResponseCompression();
            app.UseResponseCaching();

            app.UseAuthentication();
            app.UseAuthorization();

            // Rate Limiting
            //app.UseRateLimiter();

            app.MapControllers();
            app.MapHealthChecks("/health");

            // Khởi tạo index Elasticsearch khi ứng dụng khởi động
            using (var scope = app.Services.CreateScope())
            {
                var elastic = scope.ServiceProvider.GetService<ElasticService>();
                if (elastic != null) await elastic.EnsureIndexAsync();
            }

            //SignalR
            app.MapHub<NotificationHub>("/notificationHub");

            await app.RunAsync();
        }
    }
}