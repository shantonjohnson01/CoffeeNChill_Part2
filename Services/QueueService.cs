using Azure.Storage.Queues;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CoffeeNChill.Functions.Services;

public class QueueService
{
    public const string QueueName = "order-processing-queue";

    private readonly QueueClient _queueClient;

    public QueueService(IConfiguration configuration)
    {
        var connectionString =
            configuration["CoffeeNChillStorage"]
            ?? configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException(
                "CoffeeNChillStorage or AzureWebJobsStorage is not configured.");

        var options = new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        };

        _queueClient = new QueueClient(
            connectionString,
            QueueName,
            options);
    }

    public async Task<string> EnqueueOrderAsync(OrderRequest request)
    {
        await _queueClient.CreateIfNotExistsAsync();

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await _queueClient.SendMessageAsync(json);

        return response.Value.MessageId;
    }
}
