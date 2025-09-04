﻿using Application.Abstractions.Repositories;
using Application.Abstractions.Repositories.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Common
{
    public interface IUnitOfWork : IDisposable
    {
        // Repository đặc thù
        IOrderRepository Orders { get; }
        IProductRepository Products { get; }
        IAuthRepository Auths { get; }

        // Generic repository
        IRepository<T> GetRepository<T>() where T : class;

        // Transaction control
        Task BeginTransactionAsync();
        Task<bool> CommitTransactionAsync();
        Task<bool> RollbackTransactionAsync();
        Task AddIntegrationEventToOutboxAsync(object integrationEvent);

        // Save changes
        /// <summary>
        /// Lưu thay đổi vào DB, trả về số bản ghi bị ảnh hưởng
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
