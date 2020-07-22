﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using coach_bags_selenium.Data;
using Flurl.Http;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium.Chrome;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.IO;
using Tweetinvi;
using Tweetinvi.Parameters;
using Tweetinvi.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using AngleSharp;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

[assembly: UserSecretsIdAttribute("35c1247a-0256-4d98-b811-eb58b6162fd7")]
namespace coach_bags_selenium
{
    class Program
    {
        static Task Main(string[] args) => CreateHostBuilder().RunCommandLineApplicationAsync<Program>(args);
                
        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient(_ => new ImageProcessor(1200, 628));
                });

        private IHostEnvironment _env;
        private Microsoft.Extensions.Configuration.IConfiguration _config;
        private readonly ImageProcessor _imageProcessor;

        public Program(IHostEnvironment env, Microsoft.Extensions.Configuration.IConfiguration config,
            ImageProcessor imageProcessor)
        {
            _env = env;
            _config = config;
            _imageProcessor = imageProcessor;
        }

        public void OnExecute()
        {
            var twitterOptions = _config.GetSection("Twitter").Get<TwitterOptions>();
            var count = _config.GetValue<int>("Count");
            var category = Enum.Parse<Category>(_config.GetValue<string>("Category"));
            var connectionString = _config.GetConnectionString("Postgres");
            var db = new coach_bags_selenium.Data.DatabaseContext(connectionString);
            db.Database.Migrate();

            var driver = ConfigureDriver();

            try
            {
                var products = category switch
                {
                    Category.CoachBags => GetCoachBags(driver, count),
                    _ => GetFwrdProducts(driver, category).Result,
                };
                
                var now = db.Save(products);
                var product = db.ChooseProductToTweet(products.First().Category, now);

                if (product != null)
                {
                    product.LastPostedUtc = now;
                    var images = _imageProcessor.GetImages(category, product).ToList();

                    var text = $"{product.Name} - {product.SavingsPercent}% off, was ${product.Price}, now ${product.SalePrice} {product.Link}";

                    Auth.SetUserCredentials(twitterOptions.ConsumerKey, twitterOptions.ConsumerSecret, twitterOptions.AccessToken, twitterOptions.AccessTokenSecret);
                    var media = UploadImagesToTwitter(images);
                    var tweet = Tweet.PublishTweet(text, new PublishTweetOptionalParameters
                    {
                        Medias = media.ToList()
                    });
                    Console.WriteLine($"Tweeted: {text}");
                    db.SaveChanges();
                }
                else 
                    Console.WriteLine("Nothing new to tweet");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                driver?.Quit();
            }
        }

        private static IEnumerable<IMedia> UploadImagesToTwitter(IEnumerable<byte[]> images)
        {
            foreach (var image in images)
                yield return Upload.UploadBinary(image);
        }

        private static IEnumerable<Product> GetCoachBags(ChromeDriver driver, int count)
        {
            var url = $"https://coachaustralia.com/on/demandware.store/Sites-au-coach-Site/en_AU/Search-UpdateGrid?cgid=sale-womens_sale-bags&start=0&sz={count}";
            driver.Navigate().GoToUrl(url);

            var products =
                driver.FindElementsByClassName("product")
                .Select(p => new CoachBag(p).AsEntity);
            Console.WriteLine($"Found {products.Count()} products");
            return products;
        }

        private async static Task<IEnumerable<Product>> GetFwrdProducts(ChromeDriver driver, Category category)
        {
            string url = GetFwrdUrl(category);
            driver.Navigate().GoToUrl(url);

            var text = driver.PageSource;
            var pre = driver.FindElementByCssSelector("pre").Text;
            var config = AngleSharp.Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(pre));
            var ps = document.QuerySelectorAll(".products-grid__item");

            var products = ps
                .Select(p => new ForwardProduct(p).AsEntity(category))
                .Where(p => p.SalePrice < 1000)
                .ToList();

            Console.WriteLine($"Found {products.Count()} products");
            return products;
        }

        private static string GetFwrdUrl(Category category)
        {
            return category switch {
                Category.FwrdShoes => "https://www.fwrd.com/fw/content/products/lazyLoadProductsForward?currentPlpUrl=https%3A%2F%2Fwww.fwrd.com%2Ffw%2FCategory.jsp%3FpageNum%3D1%26sortBy%3DnewestMarkdown%26aliasURL%3Dsale-category-shoes%2Fba4e3d%26site%3Df%26%26n%3Ds%26s%3Dc%26c%3DShoes&currentPageSortBy=newestMarkdown&useLargerImages=true&outfitViewSession=false&showBagSize=false&lookfwrd=false",
                Category.FwrdDresses => "https://www.fwrd.com/fw/content/products/lazyLoadProductsForward?currentPlpUrl=https%3A%2F%2Fwww.fwrd.com%2Ffw%2FCategory.jsp%3Fnavsrc%3Dleft%26pageNum%3D1%26sortBy%3DnewestMarkdown%26aliasURL%3Dsale-category-dresses%2F28a4b1%26site%3Df%26%26n%3Ds%26s%3Dc%26c%3DDresses&currentPageSortBy=newestMarkdown&useLargerImages=false&outfitViewSession=false&showBagSize=false&lookfwrd=false",
                Category.FwrdBags => "https://www.fwrd.com/fw/content/products/lazyLoadProductsForward?currentPlpUrl=https%3A%2F%2Fwww.fwrd.com%2Ffw%2FCategory.jsp%3Fnavsrc%3Dleft%26pageNum%3D1%26sortBy%3DnewestMarkdown%26aliasURL%3Dsale-category-bags%2F01ef40%26site%3Df%26%26n%3Ds%26s%3Dc%26c%3DBags&currentPageSortBy=newestMarkdown&useLargerImages=true&outfitViewSession=false&showBagSize=false&lookfwrd=false",
                _ => throw new ArgumentException(nameof(category))
            };
        }

        private static ChromeDriver ConfigureDriver()
        {
            var options = new ChromeOptions();
            options.AddArgument("headless");
            options.AddArgument("no-sandbox"); // needed to run inside container

            var driver = new ChromeDriver(options);
            driver.ExecuteChromeCommand("Network.setUserAgentOverride", new System.Collections.Generic.Dictionary<string, object> {
                { "userAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.53 Safari/537.36" }
            });
            driver.ExecuteChromeCommand("Page.addScriptToEvaluateOnNewDocument",
                new System.Collections.Generic.Dictionary<string, object> {
                    { "source", @"
                        Object.defineProperty(navigator, 'webdriver', {
                            get: () => undefined
                        })"
                    }
                 }
            );
            return driver;
        }
    }
}
