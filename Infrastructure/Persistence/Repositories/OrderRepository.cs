using Application.Abstractions.Repositories;
using Domain.Entities;
using Domain.Entities.Identity;
using Infrastructure.Persistence.DatabaseContext;
using Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
            => await _dbSet.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }
}
