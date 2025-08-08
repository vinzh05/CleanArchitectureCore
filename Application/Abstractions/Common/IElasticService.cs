using Domain.Entities.Identity;
using Nest;
using System;
using System.Threading.Tasks;

namespace Application.Abstractions.Common
{
    public interface IElasticService
    {
        Task EnsureIndexAsync(string indexName = "products");
        Task IndexAsync<T>(T doc, string index = "products") where T : class;
        //Task<ISearchResponse<T>> SearchAsync<T>(string q, int from = 0, int size = 20) where T : class;
        Task<ISearchResponse<Product>> SearchAsync(string q, int from = 0, int size = 20);
        Task DeleteAsync<T>(string id, string index = "products") where T : class;
    }
}