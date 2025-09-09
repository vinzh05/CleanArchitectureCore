using Application.Abstractions.Repositories.Common;
using Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<IEnumerable<Payment>> GetExpiredPaymentsAsync(DateTime now);
    }
}
