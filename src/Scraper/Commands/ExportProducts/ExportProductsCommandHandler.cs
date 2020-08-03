using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using McMaster.Extensions.CommandLineUtils;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace coach_bags_selenium
{
    [Command("generate")]
    public class ExportProductsCommandHandler : IRequestHandler<ExportProductsCommand> {
        private readonly DataFactory _data;
        private readonly ILogger<ExportProductsCommandHandler> _logger;
        const string LOCAL_DIRECTORY = "json";

        public ExportProductsCommandHandler(DataFactory data, ILogger<ExportProductsCommandHandler> logger)
        {
            _data = data;
            _logger = logger;
        }

        class LinkedProduct : Data.Product
        {
            const string FORMAT = "yyyyMMdd-HHmm";

            [JsonIgnore]
            public DateTime? PrevPostedUtc { get; set; }

            [JsonIgnore]
            public string NextPage => PrevPostedUtc?.ToString(FORMAT);

            [JsonIgnore]
            public string PageName => LastPostedUtc?.ToString(FORMAT);
        }

        private string QUERY = @"
            with cte as (
                SELECT * FROM products
                WHERE last_posted_utc IS NOT NULL
                ORDER BY last_posted_utc 
            )
            SELECT *,
            LAG(last_posted_utc,1) OVER (
                    ORDER BY last_posted_utc
                ) prev_posted_utc
            FROM cte
            ORDER BY last_posted_utc DESC";

        class Page
        {
            [JsonIgnore]
            public string Name { get; set; }
            public LinkedProduct[] Products { get; set; }
            public string NextPage => Products.Last().NextPage;
        }
        public async Task<Unit> Handle(ExportProductsCommand request, CancellationToken cancellationToken)
        {
            var pages = new List<Page>();

            var records = await GetProducts();

            var indexPageSize = records.Count() % request.PageSize is var ix && ix == 0 ? request.PageSize : ix;

            var totalPages = records.Count() / request.PageSize + (indexPageSize == request.PageSize ? 0 : 1);
            
            _logger.LogInformation($"Total records: {records.Count()}, total pages: {totalPages}");

            pages.AddRange(new[] {
                new Page { Name = "index.json", Products = records.Take(indexPageSize).ToArray() },
                GetPage(records, request.PageSize, indexPageSize),
            });

            if (request.All)
            {
                var remainingPageCount = totalPages - 2;
                var firstPages = pages.SelectMany(p => p.Products).Count();
                for (int i = 0; i < remainingPageCount; i++)
                {
                    pages.Add(GetPage(records, request.PageSize, firstPages + request.PageSize * i));
                }
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                IgnoreNullValues = true,
            };

            Directory.CreateDirectory(LOCAL_DIRECTORY);

            foreach (var page in pages)
            {
                var json = JsonSerializer.Serialize(page, options);
                File.WriteAllText(Path.Combine(LOCAL_DIRECTORY, page.Name), json);
            }

            return Unit.Value;
        }

        private static Page GetPage(IEnumerable<LinkedProduct> records, int pageSize, int skip)
        {
            return new Page
            { 
                Name = $"{records.Skip(skip).First().PageName}.json", 
                Products = records.Skip(skip).Take(pageSize).ToArray()
            };
        }

        private async Task<IEnumerable<LinkedProduct>> GetProducts()
        {
            using (var connection = _data.GetConnection())
            {
                var result = await connection.QueryAsync<LinkedProduct>(QUERY);
                return result;
            }
        }
    }
}
