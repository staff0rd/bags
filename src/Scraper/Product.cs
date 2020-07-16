using OpenQA.Selenium;

namespace coach_bags_selenium
{
    public class Product
    {
        private readonly IWebElement _element;
        public string Link => _element.FindElement(By.CssSelector(".card-img a")).GetAttribute("href");
        public string Name => _element.FindElement(By.ClassName("product-tile-name")).Text;
        public decimal SalePrice => decimal.Parse(_element.FindElement(By.CssSelector(".sales .value")).GetAttribute("content"));
        public decimal Price => decimal.Parse(_element.FindElement(By.CssSelector(".strike-through .value")).GetAttribute("content"));
        public decimal Savings => Price - SalePrice;
        public string Id => _element.GetAttribute("data-pid");
        public string Image => _element.FindElement(By.ClassName("card-img-top")).GetAttribute("src");

        public Product(IWebElement element)
        {
            _element = element;
        }

        public coach_bags_selenium.Data.Product AsEntity => new Data.Product
        {
            Link = Link,
            Name = Name,
            SalePrice = SalePrice,
            Price = Price,
            Savings = Savings,
            Id = Id,
        };
    }
}
