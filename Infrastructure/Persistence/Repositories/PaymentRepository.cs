using Application.Abstractions.Repositories;
using Domain.Entities.Identity;
using Domain.Enums;
using Infrastructure.Persistence.DatabaseContext;
using Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Payment>> GetExpiredPaymentsAsync(DateTime now)
        {
            return await _dbSet.Where(p => p.Status == PaymentStatus.Pending && p.ExpiryDate < now).ToListAsync();
        }
    }
}
