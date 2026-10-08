using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoffeeNChill.Functions.Functions;

public class MenuFunctions
{
    private readonly MenuTableService _menuTableService;
    private readonly ILogger<MenuFunctions> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public MenuFunctions(
        MenuTableService menuTableService,
        ILogger<MenuFunctions> logger)
    {
        _menuTableService = menuTableService;
        _logger = logger;
    }

    [Function("CreateMenuItem")]
    public async Task<IActionResult> CreateMenuItem(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "menu")]
        HttpRequest req)
    {
        try
        {
            var request = await JsonSerializer.DeserializeAsync<MenuItemRequest>(
                req.Body,
                JsonOptions);

            if (request is null)
            {
                return new BadRequestObjectResult(new
                {
                    error = "Request body is required."
                });
            }

            var validationError = ValidateCreateRequest(request);

            if (validationError is not null)
            {
                return new BadRequestObjectResult(new
                {
                    error = validationError
                });
            }

            var existing = await _menuTableService.GetAsync(
                request.Category.Trim(),
                request.Id.Trim());

            if (existing is not null)
            {
                return new ConflictObjectResult(new
                {
                    error = "A menu item with this category and ID already exists."
                });
            }

            var item = new MenuItem
            {
                PartitionKey = request.Category.Trim(),
                RowKey = request.Id.Trim(),
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                Price = request.Price,
                IsAvailable = request.IsAvailable
            };

            var created = await _menuTableService.CreateAsync(item);

            var location =
                $"/api/menu/{Uri.EscapeDataString(created.PartitionKey)}/{Uri.EscapeDataString(created.RowKey)}";

            return new CreatedResult(location, created);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new
            {
                error = "Invalid JSON request body."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create menu item.");

            return new ObjectResult(new
            {
                error = "An unexpected error occurred while creating the menu item."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("GetAllMenuItems")]
    public async Task<IActionResult> GetAllMenuItems(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "menu")]
        HttpRequest req)
    {
        try
        {
            var items = await _menuTableService.GetAllAsync();
            return new OkObjectResult(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve menu items.");

            return new ObjectResult(new
            {
                error = "An unexpected error occurred while retrieving menu items."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("GetMenuItemsByCategory")]
    public async Task<IActionResult> GetMenuItemsByCategory(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "menu/category/{category}")]
        HttpRequest req,
        string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return new BadRequestObjectResult(new
            {
                error = "Category is required."
            });
        }

        try
        {
            var items = await _menuTableService.GetByCategoryAsync(category.Trim());

            return new OkObjectResult(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve menu items for category {Category}.",
                category);

            return new ObjectResult(new
            {
                error = "An unexpected error occurred while filtering menu items."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("GetMenuItemById")]
    public async Task<IActionResult> GetMenuItemById(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "menu/{category}/{id}")]
        HttpRequest req,
        string category,
        string id)
    {
        if (string.IsNullOrWhiteSpace(category) ||
            string.IsNullOrWhiteSpace(id))
        {
            return new BadRequestObjectResult(new
            {
                error = "Category and ID are required."
            });
        }

        try
        {
            var item = await _menuTableService.GetAsync(
                category.Trim(),
                id.Trim());

            if (item is null)
            {
                return new NotFoundObjectResult(new
                {
                    error = "Menu item not found."
                });
            }

            return new OkObjectResult(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve menu item {Category}/{Id}.",
                category,
                id);

            return new ObjectResult(new
            {
                error = "An unexpected error occurred while retrieving the menu item."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("UpdateMenuItem")]
    public async Task<IActionResult> UpdateMenuItem(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "put",
            Route = "menu/{category}/{id}")]
        HttpRequest req,
        string category,
        string id)
    {
        if (string.IsNullOrWhiteSpace(category) ||
            string.IsNullOrWhiteSpace(id))
        {
            return new BadRequestObjectResult(new
            {
                error = "Category and ID are required."
            });
        }

        try
        {
            var request =
                await JsonSerializer.DeserializeAsync<MenuItemUpdateRequest>(
                    req.Body,
                    JsonOptions);

            if (request is null)
            {
                return new BadRequestObjectResult(new
                {
                    error = "Request body is required."
                });
            }

            if (!request.Price.HasValue && !request.IsAvailable.HasValue)
            {
                return new BadRequestObjectResult(new
                {
                    error = "At least one of Price or IsAvailable must be supplied."
                });
            }

            if (request.Price.HasValue && request.Price.Value < 0)
            {
                return new BadRequestObjectResult(new
                {
                    error = "Price cannot be negative."
                });
            }

            var updated = await _menuTableService.UpdateAsync(
                category.Trim(),
                id.Trim(),
                request);

            if (!updated)
            {
                return new NotFoundObjectResult(new
                {
                    error = "Menu item not found."
                });
            }

            var result = await _menuTableService.GetAsync(
                category.Trim(),
                id.Trim());

            return new OkObjectResult(result);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new
            {
                error = "Invalid JSON request body."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update menu item {Category}/{Id}.",
                category,
                id);

            return new ObjectResult(new
            {
                error = "An unexpected error occurred while updating the menu item."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("DeleteMenuItem")]
    public async Task<IActionResult> DeleteMenuItem(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "menu/{category}/{id}")]
        HttpRequest req,
        string category,
        string id)
    {
        if (string.IsNullOrWhiteSpace(category) ||
            string.IsNullOrWhiteSpace(id))
        {
            return new BadRequestObjectResult(new
            {
                error = "Category and ID are required."
            });
        }

        try
        {
            var deleted = await _menuTableService.DeleteAsync(
                category.Trim(),
                id.Trim());

            if (!deleted)
            {
                return new NotFoundObjectResult(new
                {
                    error = "Menu item not found."
                });
            }

            return new NoContentResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete menu item {Category}/{Id}.",
                category,
                id);

            return new ObjectResult(new
            {
                error = "An unexpected error occurred while deleting the menu item."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    private static string? ValidateCreateRequest(MenuItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return "Category is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            return "ID/SKU is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required.";
        }

        if (request.Price < 0)
        {
            return "Price cannot be negative.";
        }

        return null;
    }
}
