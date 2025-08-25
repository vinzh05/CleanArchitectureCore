using Application.Abstractions.Infrastructure;
using Domain.Entities.Identity;
using Elasticsearch.Net;
using Nest;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Search
{
    public class ElasticService : IElasticService
    {
        private readonly IElasticClient _client;

        public ElasticService(IElasticClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            EnsureIndexAsync().GetAwaiter().GetResult(); // Khởi tạo index khi service khởi động
        }

        public async Task EnsureIndexAsync(string indexName = "products")
        {
            var exists = await _client.Indices.ExistsAsync(indexName);
            if (!exists.Exists)
            {
                await _client.Indices.CreateAsync(indexName, c => c
                    .Map<Product>(m => m.AutoMap())
                    .Settings(s => s
                        .NumberOfShards(1)
                        .NumberOfReplicas(0)
                        .Analysis(a => a
                            .Analyzers(an => an
                                .Custom("custom_analyzer", ca => ca
                                    .Tokenizer("standard")
                                    .Filters("lowercase", "asciifolding")
                                )
                            )
                        )
                    )
                );
            }
        }

        public async Task IndexAsync<T>(T doc, string index = "products") where T : class
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            await _client.IndexAsync(doc, i => i
                .Index(index)
                .Refresh(Refresh.WaitFor)
                .Pipeline("default"));
        }

        //public async Task<ISearchResponse<T>> SearchAsync<T>(string q, int from = 0, int size = 20) where T : class
        //{
        //    if (string.IsNullOrWhiteSpace(q)) throw new ArgumentException("Query cannot be empty.", nameof(q));
        //    return await _client.SearchAsync<T>(s => s
        //        .Index("products")
        //        .From(from)
        //        .Size(size)
        //        .Query(qry => qry
        //            .MultiMatch(mm => mm
        //                .Fields(f => f
        //                    .Field(p => p.Name)
        //                    .Field(p => p.Description)
        //                )
        //                .Query(q)
        //                .Fuzziness(Fuzziness.Auto)
        //            )
        //        )
        //        .Highlight(h => h
        //            .PreTags("<strong>")
        //            .PostTags("</strong>")
        //            .Fields(fs => fs
        //                .Field("*")
        //            )
        //        )
        //    );
        //}

        public async Task<ISearchResponse<Product>> SearchAsync(string q, int from = 0, int size = 20)
        {
            if (string.IsNullOrWhiteSpace(q)) throw new ArgumentException("Query cannot be empty.", nameof(q));
            return await _client.SearchAsync<Product>(s => s
                .Index("products")
                .From(from)
                .Size(size)
                .Query(qry => qry
                    .MultiMatch(mm => mm
                        .Fields(f => f
                            .Field(p => p.Name)
                            .Field(p => p.Description)
                        )
                        .Query(q)
                        .Fuzziness(Fuzziness.Auto)
                    )
                )
                .Highlight(h => h
                    .PreTags("<strong>")
                    .PostTags("</strong>")
                    .Fields(fs => fs
                        .Field("*")
                    )
                )
            );
        }

        public async Task DeleteAsync<T>(string id, string index = "products") where T : class
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID cannot be empty.", nameof(id));
            await _client.DeleteAsync<T>(new Id(id), d => d
                .Index(index)
                .Refresh(Refresh.WaitFor));
        }
    }
}