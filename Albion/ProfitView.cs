namespace Albion;

public record ProfitView(
    Resource Product,
    Recipe Recipe,
    double SellPrice,
    double Revenue,
    double IngredientCost,
    double AverageIngredientCost,
    double PercentDifference,
    double Cost,
    double FocusCost,
    double Profit)
{
    public double ProfitPerFocus => Profit / FocusCost;
}