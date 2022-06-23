using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Albion;


public static class RefiningCalculator
{

    public sealed class RefiningCalculatorWithPrices
    {
        // "wood", "fiber", etc. aren't products.
        private static readonly HashSet<string> ProductSubcategories = new()
        {
            "planks", "cloth", "stoneblock", "leather", "metalbar"
        };
        
        private static readonly Dictionary<int, int> TierToFocusCost = new()
        {
            [2] = 10,
            [3] = 24,
            [4] = 48,
            [5] = 89,
            [6] = 160,
            [7] = 284,
            [8] = 500,
        };

        private static double MarketTax => 0.03;
        private static double SetupFee => 0.015;
        private static double MarketCost => MarketTax + SetupFee;
        
        private static double NutritionPerItemValue => 0.1125;
        
        private IReadOnlyCollection<Resource> Resources { get; }
        private Dictionary<string, int> UsageFees { get; }
        
        private Dictionary<string, ItemPrice> PriceDict { get; }

        private static double Rrr(bool useFocus) => useFocus ? 53.9 : 36.7;

        private static double RecipeOutput(double rrr)
        {
            var rrrFraction = rrr / 100;
            return 1 / (1 - rrrFraction);
        }

        private static double NutritionConsumed(Resource product) => 
            product.ItemValue * NutritionPerItemValue;

        private double RefiningCost(Resource product)
        {
            var usageFee = UsageFees[product.Subcategory];
            // Usage fees are expressed per 100 nutrition.
            var usageFeePerNutrition = usageFee / 100;
            var nutritionConsumed = NutritionConsumed(product);
            
            // Tier 2 products don't have any refining cost.
            return product.Tier == 2 ? 0 : usageFeePerNutrition * nutritionConsumed;
        }

        private static double FocusCost(Resource product) => TierToFocusCost[product.Tier];

        private static double PercentDifference(double a, double b) =>
            Math.Abs(a - b) / Math.Max(a, b) * 100;

        private ProfitView RecipeProfit(
            Recipe recipe,
            Resource product,
            bool useFocus)
        {
            var sellPrice = PriceDict[product.Name].SellPrice;
            var output = RecipeOutput(Rrr(useFocus));
            var revenue = sellPrice * output;

            var ingredientCost = recipe.Ingredients
                .Select(ingredient => ingredient.Count * PriceDict[ingredient.ItemName].SellPrice)
                .Aggregate(0, (total, price) => total + price);
            var averageIngredientCost = recipe.Ingredients
                .Select(ingredient => ingredient.Count * PriceDict[ingredient.ItemName].AveragePrice)
                .Aggregate(0, (total, price) => total + price);
            var percentDifference = PercentDifference(ingredientCost, averageIngredientCost);

            var refiningCost = RefiningCost(product);
            var cost = ingredientCost + refiningCost + MarketCost;

            var focusCost = FocusCost(product);

            var profit = revenue - cost;

            var profitView = new ProfitView(
                product,
                recipe,
                sellPrice,
                revenue,
                ingredientCost,
                averageIngredientCost,
                percentDifference,
                cost,
                focusCost,
                profit);

            return profitView;
        }
        
        public RefiningCalculatorWithPrices(IReadOnlyCollection<Resource> resources, Dictionary<string, int> usageFees, Dictionary<string, ItemPrice> priceDict)
        {
            Resources = resources;
            UsageFees = usageFees;
            PriceDict = priceDict;
        }

        public IReadOnlyList<ProfitView> RefiningProfits(bool useFocus)
        {
            var products = Resources
                .Where(res => ProductSubcategories.Contains(res.Subcategory) && res.Recipes.Any())
                .Select(res => res with
                {Recipes = res.Recipes
                    .Where(recipe => recipe.Ingredients.All(ingredient => PriceDict.ContainsKey(ingredient.ItemName)))
                    .ToList()});

            return products
                .SelectMany(product => 
                    product.Recipes.Select(recipe => 
                        RecipeProfit(recipe, product, useFocus)))
                .ToImmutableList();
        }
    }

    public static async Task<RefiningCalculatorWithPrices> 
        CreateRefiningCalculator(IReadOnlyCollection<Resource> resources, Dictionary<string, int> usageFees)
    {
        var prices = await MarketPrices.FetchPrices(resources);
        var priceDict = prices.ToDictionary(price => price.ItemName, price => price);
        return new RefiningCalculatorWithPrices(resources, usageFees, priceDict);
    }
}