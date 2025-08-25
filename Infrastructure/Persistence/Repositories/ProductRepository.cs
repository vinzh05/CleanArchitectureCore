using Application.Abstractions.Repositories;
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
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<Product>> SearchByNameAsync(string q, int skip = 0, int take = 20)
        {
            if (string.IsNullOrWhiteSpace(q))
                return await _dbSet.Skip(skip).Take(take).ToListAsync();

            return await _dbSet
                .Where(p => EF.Functions.Like(p.Name, $"%{q}%") || EF.Functions.Like(p.Description, $"%{q}%"))
                .Skip(skip).Take(take).ToListAsync();
        }
    }
}
