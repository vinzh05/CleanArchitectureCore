using Application.Contracts.Payment;
using Shared.Common;
using System;
using System.Threading.Tasks;

namespace Application.Abstractions.Service
{
    public interface IPaymentService
    {
        Task<Result<PaymentResponse>> ProcessPaymentAsync(PaymentRequest request);
        Task<Result<PaymentResponse>> GetPaymentByIdAsync(Guid id);
        Task<Result<bool>> CompletePaymentAsync(Guid id);
        Task<Result<bool>> FailPaymentAsync(Guid id);
    }
}
