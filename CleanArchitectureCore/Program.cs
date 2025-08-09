using Ecom.Infrastructure.DI;
using Infrastructure.DI;
using Infrastructure.Search;
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
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Đăng ký toàn bộ infrastructure
            builder.Services.AddInfrastructure(builder.Configuration);

            // Tự động đăng ký tất cả các dịch vụ của tầng Application
            builder.Services.AddApplicationServices();

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
            await app.RunAsync();
        }
    }
}