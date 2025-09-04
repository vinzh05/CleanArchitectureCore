
using Application.Abstractions.Repositories;
using Domain.Entities.Identity;
using Infrastructure.Persistence.DatabaseContext;
using Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class AuthRepository : Repository<User>, IAuthRepository
    {
        public AuthRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<User?> GetByEmail(string email) => await _dbSet.AsNoTracking().FirstOrDefaultAsync(o => o.Email == email);
    }
}


//using Application.Abstractions.Repositories;
//using Domain.Entities.Identity;
//using Infrastructure.Persistence.Repositories.Common;
//using System.Threading.Tasks;

//namespace Infrastructure.Persistence.Repositories
//{
//    public class AuthRepository : Repository<User>, IAuthRepository
//    {
//        private readonly string _connectionString;

//        public AuthRepository(string connectionString) : base(connectionString)
//        {
//            _connectionString = connectionString;
//        }

//        public async Task<User?> GetByEmail(string email)
//        {
//            string storeName = "sp_User_GetByEmail";
//            List<User> result;
//            string error = DBM.GetList(_connectionString, storeName, new { Email = email }, out result);
//            if (!string.IsNullOrEmpty(error))
//            {
//                throw new Exception(error);
//            }
//            return result.FirstOrDefault();
//        }
//    }
//}