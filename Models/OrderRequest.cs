namespace CoffeeNChill.Functions.Models;

public class OrderRequest
{
    public string OrderId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public List<string> SelectedItemSKUs { get; set; } = new();

    public double TotalPrice { get; set; }

    public DateTimeOffset OrderTimestamp { get; set; }
}
