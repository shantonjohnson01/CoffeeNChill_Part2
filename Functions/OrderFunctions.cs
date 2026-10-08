using System.Net;
using System.Text.Json;
using Azure;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public class OrderFunctions
{
    private readonly QueueService _queueService;
    private readonly OrderTableService _orderTableService;
    private readonly ILogger<OrderFunctions> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<char> InvalidOrderIdCharacters = new()
    {
        '/', '\\', '#', '?'
    };

    public OrderFunctions(
        QueueService queueService,
        OrderTableService orderTableService,
        ILogger<OrderFunctions> logger)
    {
        _queueService = queueService;
        _orderTableService = orderTableService;
        _logger = logger;
    }

    [Function("QueueOrder")]
    public async Task<HttpResponseData> QueueOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/queue")]
        HttpRequestData req)
    {
        try
        {
            var request = await JsonSerializer.DeserializeAsync<OrderRequest>(
                req.Body,
                JsonOptions);

            var validationError = Validate(request);

            if (validationError is not null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new
                {
                    error = validationError
                });
                return badRequest;
            }

            request!.OrderTimestamp = request.OrderTimestamp.ToUniversalTime();

            var messageId = await _queueService.EnqueueOrderAsync(request);

            _logger.LogInformation(
                "Order {OrderId} queued with queue message {MessageId}.",
                request.OrderId,
                messageId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                message = "Order accepted for asynchronous processing.",
                orderId = request.OrderId,
                queue = QueueService.QueueName,
                queueMessageId = messageId,
                status = "Queued"
            });

            return response;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON submitted to order queue endpoint.");

            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(new
            {
                error = "Request body must contain valid JSON."
            });
            return response;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Queue Storage failed while queuing an order.");

            var response = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            await response.WriteAsJsonAsync(new
            {
                error = "The order queue is temporarily unavailable."
            });
            return response;
        }
    }

    [Function("GetOrder")]
    public async Task<HttpResponseData> GetOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{orderDate}/{orderId}")]
        HttpRequestData req,
        string orderDate,
        string orderId)
    {
        try
        {
            var order = await _orderTableService.GetAsync(orderDate, orderId);

            if (order is null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteAsJsonAsync(new
                {
                    error = $"Order '{orderId}' was not found for date '{orderDate}'."
                });
                return notFound;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ToResponse(order));
            return response;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to read order {OrderId}.", orderId);

            var response = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            await response.WriteAsJsonAsync(new
            {
                error = "Orders storage is temporarily unavailable."
            });
            return response;
        }
    }

    [Function("GetOrders")]
    public async Task<HttpResponseData> GetOrders(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")]
        HttpRequestData req)
    {
        try
        {
            var orders = await _orderTableService.GetAllAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(orders.Select(ToResponse));
            return response;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to list Orders table records.");

            var response = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            await response.WriteAsJsonAsync(new
            {
                error = "Orders storage is temporarily unavailable."
            });
            return response;
        }
    }

    [Function("ProcessOrderQueue")]
    public async Task ProcessOrderQueue(
        [QueueTrigger(QueueService.QueueName, Connection = "CoffeeNChillStorage")]
        string message)
    {
        OrderRequest? request = null;

        try
        {
            request = JsonSerializer.Deserialize<OrderRequest>(message, JsonOptions);

            var validationError = Validate(request);
            if (validationError is not null)
            {
                throw new InvalidDataException(validationError);
            }

            request!.OrderTimestamp = request.OrderTimestamp.ToUniversalTime();

            var order = new Order
            {
                PartitionKey = request.OrderTimestamp.UtcDateTime.ToString("yyyy-MM-dd"),
                RowKey = request.OrderId,
                OrderId = request.OrderId,
                CustomerName = request.CustomerName,
                SelectedItemSKUsJson = JsonSerializer.Serialize(request.SelectedItemSKUs),
                TotalPrice = request.TotalPrice,
                OrderTimestamp = request.OrderTimestamp,
                Status = "Received",
                StatusUpdatedAt = DateTimeOffset.UtcNow
            };

            await _orderTableService.UpsertAsync(order);

            _logger.LogInformation(
                "Order {OrderId} stored in Orders table with status Received.",
                order.OrderId);

            await AdvanceStatusAsync(order, "Preparing");
            await AdvanceStatusAsync(order, "Ready");
            await AdvanceStatusAsync(order, "Collected");

            _logger.LogInformation(
                "Order {OrderId} completed lifecycle processing.",
                order.OrderId);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Malformed queue message. The message will be retried and can eventually move to {PoisonQueue}.",
                $"{QueueService.QueueName}-poison");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Order queue processing failed for {OrderId}. The Functions runtime will retry the message; repeated failures are sent to the poison queue.",
                request?.OrderId ?? "unknown");
            throw;
        }
    }

    private async Task AdvanceStatusAsync(Order order, string status)
    {
        order.Status = status;
        order.StatusUpdatedAt = DateTimeOffset.UtcNow;

        await _orderTableService.UpsertAsync(order);

        _logger.LogInformation(
            "Order {OrderId} status changed to {Status}.",
            order.OrderId,
            status);

        await Task.Delay(TimeSpan.FromMilliseconds(750));
    }

    private static string? Validate(OrderRequest? request)
    {
        if (request is null)
        {
            return "Request body is required.";
        }

        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            return "OrderId is required.";
        }

        if (request.OrderId.Length > 100 ||
            request.OrderId.Any(c => InvalidOrderIdCharacters.Contains(c)))
        {
            return "OrderId must be 1-100 characters and cannot contain /, \\, #, or ?.";
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return "CustomerName is required.";
        }

        if (request.CustomerName.Length > 200)
        {
            return "CustomerName must not exceed 200 characters.";
        }

        if (request.SelectedItemSKUs is null || request.SelectedItemSKUs.Count == 0)
        {
            return "SelectedItemSKUs must contain at least one item SKU.";
        }

        if (request.SelectedItemSKUs.Any(string.IsNullOrWhiteSpace))
        {
            return "SelectedItemSKUs cannot contain empty values.";
        }

        if (request.TotalPrice <= 0 || double.IsNaN(request.TotalPrice) || double.IsInfinity(request.TotalPrice))
        {
            return "TotalPrice must be greater than zero and must be a finite number.";
        }

        if (request.OrderTimestamp == default)
        {
            return "OrderTimestamp is required and must be a valid ISO-8601 timestamp.";
        }

        return null;
    }

    private static object ToResponse(Order order)
    {
        List<string> skus;

        try
        {
            skus = JsonSerializer.Deserialize<List<string>>(order.SelectedItemSKUsJson)
                   ?? new List<string>();
        }
        catch
        {
            skus = new List<string>();
        }

        return new
        {
            orderId = order.OrderId,
            orderDate = order.PartitionKey,
            customerName = order.CustomerName,
            selectedItemSKUs = skus,
            totalPrice = order.TotalPrice,
            orderTimestamp = order.OrderTimestamp,
            status = order.Status,
            statusUpdatedAt = order.StatusUpdatedAt
        };
    }
}
