using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChill.Functions.Services;

public class MenuTableService
{
    private const string TableName = "MenuItems";

    private readonly TableClient _tableClient;

    public MenuTableService(IConfiguration configuration)
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

    public async Task<MenuItem> CreateAsync(MenuItem item)
    {
        await EnsureTableExistsAsync();
        await _tableClient.AddEntityAsync(item);
        return item;
    }

    public async Task<List<MenuItem>> GetAllAsync()
    {
        await EnsureTableExistsAsync();

        var results = new List<MenuItem>();

        await foreach (var item in _tableClient.QueryAsync<MenuItem>())
        {
            results.Add(item);
        }

        return results
            .OrderBy(x => x.PartitionKey)
            .ThenBy(x => x.RowKey)
            .ToList();
    }

    public async Task<List<MenuItem>> GetByCategoryAsync(string category)
    {
        await EnsureTableExistsAsync();

        var results = new List<MenuItem>();

        await foreach (var item in _tableClient.QueryAsync<MenuItem>(
            x => x.PartitionKey == category))
        {
            results.Add(item);
        }

        return results
            .OrderBy(x => x.RowKey)
            .ToList();
    }

    public async Task<MenuItem?> GetAsync(string category, string id)
    {
        await EnsureTableExistsAsync();

        try
        {
            var response = await _tableClient.GetEntityAsync<MenuItem>(
                category,
                id);

            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> UpdateAsync(
        string category,
        string id,
        MenuItemUpdateRequest update)
    {
        await EnsureTableExistsAsync();

        var existing = await GetAsync(category, id);

        if (existing is null)
        {
            return false;
        }

        if (update.Price.HasValue)
        {
            existing.Price = update.Price.Value;
        }

        if (update.IsAvailable.HasValue)
        {
            existing.IsAvailable = update.IsAvailable.Value;
        }

        await _tableClient.UpdateEntityAsync(
            existing,
            existing.ETag,
            TableUpdateMode.Replace);

        return true;
    }

    public async Task<bool> DeleteAsync(string category, string id)
    {
        await EnsureTableExistsAsync();

        var existing = await GetAsync(category, id);

        if (existing is null)
        {
            return false;
        }

        await _tableClient.DeleteEntityAsync(
            existing.PartitionKey,
            existing.RowKey,
            existing.ETag);

        return true;
    }
}
