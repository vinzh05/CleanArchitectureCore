using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.DI
{
    public static class ServiceRegistrationExtensions
    {
        /// <summary>
        /// Tự động đăng ký các service và repository dựa trên quy ước đặt tên.
        /// </summary>
        /// <param name="services">Đối tượng IServiceCollection.</param>
        /// <returns>Đối tượng IServiceCollection đã được cập nhật.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Lấy assembly chứa các interfaces của Application Layer
            var applicationAssembly = typeof(Application.Abstractions.IOrderRepository).Assembly;

            // Lấy assembly chứa các implementation của Application Layer
            var implementationAssembly = Assembly.GetExecutingAssembly();

            // Tìm và đăng ký các service có interface kết thúc bằng "Service"
            var serviceInterfaces = applicationAssembly.GetTypes()
                .Where(t => t.IsInterface && t.Name.EndsWith("Service"));

            foreach (var serviceInterface in serviceInterfaces)
            {
                var implementation = implementationAssembly.GetTypes()
                    .FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.Name == serviceInterface.Name.Substring(1));

                if (implementation != null)
                {
                    services.AddScoped(serviceInterface, implementation);
                }
            }

            return services;
        }
    }
}
