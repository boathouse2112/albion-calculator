using Newtonsoft.Json.Linq;

namespace Albion
{
    public record Ingredient(string ItemName, int Count);

    public record Recipe(int Silver, List<Ingredient> Ingredients);

    public record Resource(
        string Name,
        string Category,
        string Subcategory,
        int Tier,
        double ItemValue,
        List<Recipe> Recipes);

    public static class Program
    {
        
        private static readonly Dictionary<string, int> UsageFees = new()
        {
            ["cloth"] = 600,
            ["leather"] = 400,
            ["metalbar"] = 2600,
            ["planks"] = 380,
            ["stoneblock"] = 800,
        };

        public static async Task Main()
        {
            var resources = GameDataReader.ReadResources().ToList();
            var refiningCalculator = await RefiningCalculator.CreateRefiningCalculator(resources, UsageFees);

            var profits = refiningCalculator.RefiningProfits(false)
                .Where(profitView => profitView.Product.Tier <= 6)
                .OrderByDescending(profitView => profitView.Profit);

            Console.WriteLine("{0,-20}{1,20}{2,20}{3,20}", "Name", "Profit", "Profit / Focus", "Percent Difference");
            foreach (var profitView in profits)
            {
                Console.WriteLine("{0,-20}{1,20:N}{2,20:N}{3,20:N}",
                    profitView.Product.Name,
                    profitView.Profit,
                    profitView.ProfitPerFocus,
                    profitView.PercentDifference);
            }
            Console.WriteLine();
        }
    }
}