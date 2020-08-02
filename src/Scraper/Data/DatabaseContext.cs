using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace coach_bags_selenium.Data
{
    public class DatabaseContext : DbContext
    {
        private readonly string _connectionString;

        public DatabaseContext()
        {
            _connectionString = "User ID=postgres;Password=example;Host=localhost;Database=coach_bags;";
        }

        public DatabaseContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(_connectionString)
                .UseSnakeCaseNamingConvention();

        public Data.Product ChooseProductToTweet(Category category, DateTime now)
        {
            var pendingProducts = this.Products
                .Where(p => p.Category == category)
                .Where(p => p.LastUpdatedUtc >= now) // still available on page
                .Where(p => p.LastPostedUtc == null) // not yet tweeted
                .ToArray();

            if (!pendingProducts.Any())
                return null;

            Random rand = new Random();
            int index = rand.Next(pendingProducts.Length);

            var entity = pendingProducts.ElementAt(index);
            return entity;
        }

        public DateTime Save(IEnumerable<Product> products)
        {
            var now = DateTime.UtcNow;

            foreach (var product in products)
            {
                product.LastUpdatedUtc = now;
                
                var existing = this.Products.FirstOrDefault(p => p.Id == product.Id && p.Category == product.Category);
                if (existing is null)
                {
                    product.CreatedUtc = now;
                    this.Products.Add(product);
                } else
                {
                    existing.Link = product.Link;
                    existing.Name = product.Name;
                    existing.SalePrice = product.SalePrice;
                    existing.Price = product.Price;
                    existing.Savings = product.Savings;
                    existing.LastUpdatedUtc = now;
                    existing.Image = product.Image;
                }
            }
            Console.WriteLine("Saving...");
            this.SaveChanges();
            return now;
        }
    }
}