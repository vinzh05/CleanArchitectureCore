using Ecom.Infrastructure.DI;
using Infrastructure.DI;
using Infrastructure.Search;
using CleanArchitectureCore.Controllers;
using System.Reflection;

namespace CleanArchitectureCore
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Cấu hình
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Đăng ký dịch vụ
            builder.Services.AddControllers();
            builder.Services.AddSignalR();
            builder.Services.AddScoped<Application.Service.INotificationHub, Services.NotificationHubAdapter>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Đăng ký toàn bộ infrastructure
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplicationServices();
            builder.Services.AddJwtAuthentication(builder.Configuration);
            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            // Cấu hình HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Khởi tạo index Elasticsearch khi ứng dụng khởi động
            using (var scope = app.Services.CreateScope())
            {
                var elastic = scope.ServiceProvider.GetService<ElasticService>();
                if (elastic != null) await elastic.EnsureIndexAsync();
            }

            app.MapControllers();
            app.MapHub<NotificationHub>("/notificationHub");
            await app.RunAsync();
        }
    }
}