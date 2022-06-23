using System.Xml.Linq;

namespace Albion;

public static class GameDataReader
{
    private static XElement ReadItems()
    {
        const string filename = "data/ao-bin-dumps/items.xml";
        var currentDirectory = Directory.GetCurrentDirectory();
        var itemsFilePath = Path.Combine(currentDirectory, filename);

        return XElement.Load(itemsFilePath);
    }

    public static IEnumerable<Resource> ReadResources()
    {
        var items = ReadItems();

        var resources = items.Elements()
            .Where(el =>
            {
                var subcategory = (string?)el.Attribute("shopcategory");
                var enchantment = el.Attribute("enchantmentlevel");
                return subcategory is not null
                       && subcategory == "resources"
                       && enchantment is null;
            })
            .Select(el =>
            {
                var name = (string?)el.Attribute("uniquename");
                var category = (string?)el.Attribute("shopcategory");
                var subcategory = (string?)el.Attribute("shopsubcategory1");
                var tier = (int?)el.Attribute("tier");
                var itemValue = (double?)el.Attribute("itemvalue");

                var recipes = el.Elements()
                    .Select(recipe =>
                    {
                        var silver = (int?)recipe.Attribute("silver")
                                     ?? throw new NullReferenceException();
                        var ingredients = recipe.Elements()
                            .Select(ingredient =>
                            {
                                var ingredientName = (string?)ingredient.Attribute("uniquename")
                                                     ?? throw new NullReferenceException();
                                var count = (int?)ingredient.Attribute("count")
                                            ?? throw new NullReferenceException();
                                return new Ingredient(ingredientName, count);
                            })
                            .ToList();

                        return new Recipe(silver, ingredients);
                    })
                    .ToList();

                if (name is not null
                    && category is not null
                    && subcategory is not null
                    && tier is not null
                    && itemValue is not null)
                {
                    return new Resource(
                        Name: name,
                        Category: category,
                        Subcategory: subcategory,
                        Tier: (int)tier,
                        ItemValue: (double)itemValue,
                        Recipes: recipes);
                }

                {
                    return null;
                }
            })
            .OfType<Resource>();

        return resources;
    }
}