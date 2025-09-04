/*
using Application.Abstractions.Repositories.Common;
using Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories
{
    public interface IAuthRepository : IRepository<User>
    {
        Task<User?> FindByEmail(string email);
    }
}
*/

using Application.Abstractions.Repositories.Common;
using Domain.Entities.Identity;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories
{
    public interface IAuthRepository : IRepository<User>
    {
        Task<User?> GetByEmail(string email);
    }
}