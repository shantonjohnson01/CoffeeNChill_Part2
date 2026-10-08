using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton<MenuTableService>();
builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddSingleton<QueueService>();
builder.Services.AddSingleton<OrderTableService>();

var host = builder.Build();

await host.RunAsync();
