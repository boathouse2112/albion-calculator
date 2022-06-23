using System.Text.Json.Serialization;

namespace Albion;

public record ItemPrice(
    string ItemName,
    string City,
    int SellPrice,
    int AveragePrice); // Average price in the last hour