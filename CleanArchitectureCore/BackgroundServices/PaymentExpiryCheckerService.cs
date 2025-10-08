using Application.Abstractions;
using Application.Abstractions.Common;
using Application.Abstractions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitectureCore.BackgroundServices
{
    /// <summary>
    /// BackgroundService để check và fail payments quá hạn mỗi 30 giây.
    /// </summary>
    public class PaymentExpiryCheckerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentExpiryCheckerService> _logger;

        public PaymentExpiryCheckerService(IServiceProvider serviceProvider, ILogger<PaymentExpiryCheckerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var cache = scope.ServiceProvider.GetRequiredService<IRedisCacheService>();

                        var expiredPayments = await unitOfWork.Payments.GetExpiredPaymentsAsync(DateTime.UtcNow);
                        foreach (var payment in expiredPayments)
                        {
                            payment.FailPayment();
                            unitOfWork.Payments.Update(payment);
                            await cache.RemoveAsync($"payment:{payment.Id}");
                            _logger.LogInformation("Payment {Id} failed due to expiry", payment.Id);
                        }

                        if (expiredPayments.Any())
                            await unitOfWork.CommitTransactionAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PaymentExpiryCheckerService");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
