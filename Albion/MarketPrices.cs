using System.Collections.Immutable;
using Newtonsoft.Json.Linq;

namespace Albion;

public static class MarketPrices
{
    private static readonly Dictionary<string, string> SubcategoryToCity = new()
    {
        ["wood"] = "Fort Sterling",
        ["planks"] = "Fort Sterling",
        ["fiber"] = "Lymhurst",
        ["cloth"] = "Lymhurst",
        ["rock"] = "Bridgewatch",
        ["stoneblock"] = "Bridgewatch",
        ["hide"] = "Martlock",
        ["leather"] = "Martlock",
        ["ore"] = "Thetford",
        ["metalbar"] = "Thetford",
    };
    
    private static readonly HttpClient Client = new();
    private const string ApiUrl = "https://www.albion-online-data.com/api/v2";
    
    // public static async Task<List<ItemPrice>> FetchAveragePrices
    
    public static async Task<ImmutableList<ItemPrice>> FetchPrices(IReadOnlyCollection<Resource> resources)
    {
        var nameToResource = resources
            .ToDictionary(res => res.Name, res => res);
        var namesString = string.Join(",", resources.Select(res => res.Name));

        var citiesString = string.Join(
            ",",
            resources.Select(res => SubcategoryToCity[res.Subcategory]).ToHashSet());

        var pricesString = await Client
            .GetStringAsync($"{ApiUrl}/stats/prices/{namesString}?locations={citiesString}&qualities=1");
        var priceArray = JArray.Parse(pricesString);

        var prices = priceArray
            .Select(price => (
                ItemName: (string?)price["item_id"] ?? throw new NullReferenceException(),
                City: (string?)price["city"] ?? throw new NullReferenceException(),
                SellPrice: (int?)price["sell_price_min"] ?? throw new NullReferenceException()
            ));
//            .GroupBy(price => price.ItemName);

        // var pricesCorrectCity = prices.Select(priceGroup =>
        // {
        //     var resource = nameToResource[priceGroup.First().ItemName];
        //     var city = SubcategoryToCity[resource.Subcategory];
        //     var result = priceGroup.ToList().First(price => price.City == city);
        //     return result;
        // });
        
        // Get average prices, and join with the price tuples into an ItemPrice
        var averagePricesString = await Client
            .GetStringAsync($"{ApiUrl}/stats/history/{namesString}?locations={citiesString}&qualities=1&time-scale=1");
        var averagePriceArray = JArray.Parse(averagePricesString);

        var averagePrices = averagePriceArray
            .Select(averagePrice => (
                ItemName: (string?)averagePrice["item_id"] ?? throw new NullReferenceException(),
                City: (string?)averagePrice["location"] ?? throw new NullReferenceException(),
                AveragePrice: (int?)averagePrice["data"]?.Last?["avg_price"] ?? throw new NullReferenceException()
            ));

        var pricesWithAverage =
            from price in prices
            join averagePrice in averagePrices on new
            {
                price.ItemName,
                price.City
            } equals new
            {
                averagePrice.ItemName,
                averagePrice.City
            }
            select new ItemPrice(price.ItemName, price.City, price.SellPrice, averagePrice.AveragePrice);
        
        var pricesWithAverageCorrectCity = pricesWithAverage
            .Where(price =>
            {
                var resource = nameToResource[price.ItemName];
                return price.City == SubcategoryToCity[resource.Subcategory];
            });
        
        // return pricesCorrectCity.ToList();
        return pricesWithAverageCorrectCity.ToImmutableList();
    }
}