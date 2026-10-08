# Stage 1: build the Azure Functions application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["CoffeeNChill.Functions.csproj", "."]
RUN dotnet restore "CoffeeNChill.Functions.csproj"

COPY . .
RUN dotnet publish "CoffeeNChill.Functions.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

# Stage 2: run the published app using the official Azure Functions .NET 8 isolated image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final

WORKDIR /home/site/wwwroot

COPY --from=build /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

EXPOSE 80
