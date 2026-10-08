using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChill.Functions.Services;

public class OrderTableService
{
    private const string TableName = "Orders";

    private readonly TableClient _tableClient;

    public OrderTableService(IConfiguration configuration)
    {
        var connectionString =
            configuration["CoffeeNChillStorage"]
            ?? configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException(
                "CoffeeNChillStorage or AzureWebJobsStorage is not configured.");

        _tableClient = new TableClient(connectionString, TableName);
    }

    public async Task EnsureTableExistsAsync()
    {
        await _tableClient.CreateIfNotExistsAsync();
    }

    public async Task UpsertAsync(Order order)
    {
        await EnsureTableExistsAsync();
        await _tableClient.UpsertEntityAsync(order, TableUpdateMode.Replace);
    }

    public async Task<Order?> GetAsync(string orderDate, string orderId)
    {
        await EnsureTableExistsAsync();

        try
        {
            var response = await _tableClient.GetEntityAsync<Order>(orderDate, orderId);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<List<Order>> GetAllAsync()
    {
        await EnsureTableExistsAsync();

        var orders = new List<Order>();

        await foreach (var order in _tableClient.QueryAsync<Order>())
        {
            orders.Add(order);
        }

        return orders
            .OrderByDescending(x => x.OrderTimestamp)
            .ToList();
    }
}
