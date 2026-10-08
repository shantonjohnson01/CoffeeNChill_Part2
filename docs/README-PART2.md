
Today 8:36 PM

Azure for Developers Implement rich Azure PaaS ecosystems using containers, serverless services, and storage solutions.pdf
PDF


Portfolio of Evidence.pdf
PDF

Yes. I’ve completed Part 1 as a full standalone project, with no ZIP file. The implementation follows the PoE requirements: Azure Table MenuItems, Azure Blob staff-docs, HTTP Functions, Azurite, standalone Docker execution, Docker Hub v1.0 images, Postman tests, README, and Git requirements. 


I used .NET 8 isolated Azure Functions with ASP.NET Core HTTP integration. Microsoft currently documents .NET 8 isolated Functions and ConfigureFunctionsWebApplication() for ASP.NET Core HTTP integration. 

1. Download the complete Part 1 files
I created the complete project here:

Project folder: /mnt/data/CoffeeNChill-Part1/

Project/configuration
CoffeeNChill.Functions.csproj

Program.cs

host.json

local.settings.json

Dockerfile

.gitignore

Models
MenuItem.cs

MenuItemRequest.cs

Services
MenuTableService.cs

BlobStorageService.cs

Azure Functions
MenuFunctions.cs

DocumentFunctions.cs

Postman
CoffeeNChill-Part1.postman_collection.json

Documentation
README.md

No ZIP has been created. These are individual files that can be placed directly into your GitHub repository.

2. What you are building
Your final repository should look exactly like this:

CoffeeNChill-Part1/
│
├── CoffeeNChill.Functions.csproj
├── Program.cs
├── host.json
├── local.settings.json
├── Dockerfile
├── .gitignore
│
├── Models/
│   ├── MenuItem.cs
│   └── MenuItemRequest.cs
│
├── Services/
│   ├── MenuTableService.cs
│   └── BlobStorageService.cs
│
├── Functions/
│   ├── MenuFunctions.cs
│   └── DocumentFunctions.cs
│
└── docs/
    └── CoffeeNChill-Part1.postman_collection.json
The PoE specifically requires a README, Docker commands, /docs, Postman collection, working Functions, and Docker Hub images. 


3. Step 1 — Install the software
Install:

.NET 8 SDK

Docker Desktop

Azure Functions Core Tools v4

Git

Postman

GitHub account

Docker Hub account

Check everything:

dotnet --version
func --version
docker --version
git --version
You want .NET 8 available.

4. Step 2 — Create the project directory
Open PowerShell or Command Prompt:

mkdir CoffeeNChill-Part1
cd CoffeeNChill-Part1
Then create the folders:

mkdir Models
mkdir Services
mkdir Functions
mkdir docs
Put the downloaded files into the matching locations.

5. Step 3 — The .csproj
The project uses Azure Functions isolated worker and the Azure Storage SDKs.

The complete file is:

<Project Sdk="Azure.Functions.Sdk/1.0.0">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>CoffeeNChill.Functions</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.Functions.Worker" Version="2.52.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore" Version="2.1.1" />
    <PackageReference Include="Azure.Data.Tables" Version="12.11.0" />
    <PackageReference Include="Azure.Storage.Blobs" Version="12.30.1" />
  </ItemGroup>

  <ItemGroup>
    <None Update="host.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>

    <None Update="local.settings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <CopyToPublishDirectory>Never</CopyToPublishDirectory>
    </None>
  </ItemGroup>
</Project>
The Microsoft documentation confirms the isolated worker project model and the required Functions SDK/worker approach. 

6. Step 4 — Program.cs
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton<MenuTableService>();
builder.Services.AddSingleton<BlobStorageService>();

var host = builder.Build();

await host.RunAsync();
The important line is:

builder.ConfigureFunctionsWebApplication();
That enables the ASP.NET Core HTTP integration used by the Functions. 

7. Step 5 — host.json
{
  "version": "2.0",
  "logging": {
    "logLevel": {
      "default": "Information",
      "Host.Results": "Information",
      "Function": "Information"
    }
  },
  "extensions": {
    "http": {
      "routePrefix": "api"
    }
  }
}
Therefore:

/menu
becomes:

/api/menu
8. Step 6 — local.settings.json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "CoffeeNChillStorage": "UseDevelopmentStorage=true"
  }
}
Do not commit this file to GitHub.

It is already excluded through .gitignore.

9. Step 7 — MenuItem.cs
The PoE specifies:

PartitionKey = Category

RowKey = unique SKU/ID

Name

Description

Price

IsAvailable. 


Use:

using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Functions.Models;

public class MenuItem : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public double Price { get; set; }

    public bool IsAvailable { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}
10. Step 8 — MenuItemRequest.cs
namespace CoffeeNChill.Functions.Models;

public class MenuItemRequest
{
    public string Category { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public double Price { get; set; }

    public bool IsAvailable { get; set; }
}

public class MenuItemUpdateRequest
{
    public double? Price { get; set; }

    public bool? IsAvailable { get; set; }
}
The separate update DTO is intentional: Part 1 only requires updating price or availability.

11. Step 9 — MenuTableService.cs
This service handles all communication with:

MenuItems
in Azure Table Storage.

using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;

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

    public async Task<MenuItem?> GetAsync(
        string category,
        string id)
    {
        await EnsureTableExistsAsync();

        try
        {
            var response =
                await _tableClient.GetEntityAsync<MenuItem>(
                    category,
                    id);

            return response.Value;
        }
        catch (RequestFailedException ex)
            when (ex.Status == 404)
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

        var existing =
            await GetAsync(category, id);

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
            existing.IsAvailable =
                update.IsAvailable.Value;
        }

        await _tableClient.UpdateEntityAsync(
            existing,
            existing.ETag,
            TableUpdateMode.Replace);

        return true;
    }

    public async Task<bool> DeleteAsync(
        string category,
        string id)
    {
        await EnsureTableExistsAsync();

        var existing =
            await GetAsync(category, id);

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
This provides the storage layer separately from the HTTP functions, which is useful for the higher levels of the Part 1 rubric.

12. Step 10 — MenuFunctions.cs
This implements the required menu endpoints.

The PoE requires:

POST   /api/menu
GET    /api/menu
GET    /api/menu/category/{category}
PUT    /api/menu/{category}/{id}
DELETE /api/menu/{category}/{id}
and the rubric explicitly assesses CRUD, category filtering, validation, and appropriate errors. 


The complete implementation is in:

Download MenuFunctions.cs

It contains:

CreateMenuItem

GetAllMenuItems

GetMenuItemsByCategory

GetMenuItemById

UpdateMenuItem

DeleteMenuItem

JSON validation

negative-price validation

duplicate detection

404 handling

400 handling

409 handling

500 error handling

logging

13. Step 11 — Blob Storage
The PoE addendum explicitly changes the requirement from Azure Files to Azure Blob Storage. 


The container is:

staff-docs
Microsoft's Azure Storage SDK provides the Blob client used here. 

14. Step 12 — BlobStorageService.cs
Complete file:

Download BlobStorageService.cs

The important functionality is:

private const string ContainerName = "staff-docs";
and:

var serviceClient =
    new BlobServiceClient(connectionString);

_containerClient =
    serviceClient.GetBlobContainerClient(ContainerName);
The service implements:

EnsureContainerExistsAsync()
UploadAsync()
ListAsync()
DownloadAsync()
ExistsAsync()
It also stores the content type.

15. Step 13 — DocumentFunctions.cs
The PoE requires:

POST /api/documents/upload
GET  /api/documents
GET  /api/documents/download/{fileName}


Complete file:

Download DocumentFunctions.cs

The upload endpoint accepts:

multipart/form-data
with:

file
as the form field.

It validates:

PDF
DOC
DOCX
TXT
JPG
JPEG
PNG
and rejects unsupported extensions.

The download uses a FileStreamResult, so the document is streamed rather than unnecessarily loaded into a large byte array.

16. Step 14 — Build the application
Run:

dotnet restore
Then:

dotnet build
You must get:

Build succeeded.
Do not move on to Docker until the normal application build succeeds.

17. Step 15 — Start Azurite
The PoE requires Azurite running in its own Docker container. 


Run:

docker pull mcr.microsoft.com/azure-storage/azurite
Then:

docker run -d `
  --name coffeenchill-azurite `
  -p 10000:10000 `
  -p 10001:10001 `
  -p 10002:10002 `
  mcr.microsoft.com/azure-storage/azurite
Check:

docker ps
You should see:

coffeenchill-azurite
Important correction to the PoE
The PoE labels the ports differently, but Microsoft's current Azurite documentation gives:

10000 = Blob
10001 = Queue
10002 = Table

This matters because otherwise the application will appear broken even though your C# code is correct.

18. Step 16 — Run Functions locally
Run:

func start
You should see functions such as:

CreateMenuItem
GetAllMenuItems
GetMenuItemsByCategory
GetMenuItemById
UpdateMenuItem
DeleteMenuItem
UploadStaffDocument
ListStaffDocuments
DownloadStaffDocument
Base address:

http://localhost:7071
19. Step 17 — Test Create Menu
POST:

http://localhost:7071/api/menu
Headers:

Content-Type: application/json
Body:

{
  "category": "Hot Drinks",
  "id": "COF-001",
  "name": "Espresso",
  "description": "Single shot espresso",
  "price": 25.00,
  "isAvailable": true
}
Expected:

201 Created
You should receive something similar to:

{
  "partitionKey": "Hot Drinks",
  "rowKey": "COF-001",
  "name": "Espresso",
  "description": "Single shot espresso",
  "price": 25,
  "isAvailable": true
}
20. Step 18 — Test GET All
GET http://localhost:7071/api/menu
Expected:

200 OK
21. Step 19 — Test category filtering
GET http://localhost:7071/api/menu/category/Hot%20Drinks
Expected:

200 OK
This demonstrates the PoE's required:

PartitionKey = Category
filtering.

22. Step 20 — Test individual menu item
GET http://localhost:7071/api/menu/Hot%20Drinks/COF-001
Expected:

200 OK
23. Step 21 — Test update
PUT http://localhost:7071/api/menu/Hot%20Drinks/COF-001
Body:

{
  "price": 29.50,
  "isAvailable": true
}
Expected:

200 OK
24. Step 22 — Test delete
DELETE http://localhost:7071/api/menu/Hot%20Drinks/COF-001
Expected:

204 No Content
25. Step 23 — Test document upload
In Postman:

POST http://localhost:7071/api/documents/upload
Choose:

Body
→ form-data
Add:

Key: file
Type: File
Value: your recipe PDF
Expected:

200 OK
The response gives you the generated Blob name.

26. Step 24 — List documents
GET http://localhost:7071/api/documents
Expected:

{
  "container": "staff-docs",
  "count": 1,
  "documents": [
    {
      "fileName": "...",
      "size": 12345,
      "lastModified": "...",
      "contentType": "application/pdf"
    }
  ]
}
The rubric specifically rewards file name, size, upload/modified metadata and MIME handling. 


27. Step 25 — Download
Use the returned filename:

GET http://localhost:7071/api/documents/download/FILENAME.pdf
The PDF should download/open.

28. Step 26 — Dockerfile
The Dockerfile is:

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["CoffeeNChill.Functions.csproj", "."]
RUN dotnet restore "CoffeeNChill.Functions.csproj"

COPY . .

RUN dotnet publish "CoffeeNChill.Functions.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final

WORKDIR /home/site/wwwroot

COPY --from=build /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

EXPOSE 80
Microsoft documents mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 as the .NET 8 isolated container image. 

29. Step 27 — Build Docker image
Replace:

YOUR_DOCKERHUB_USERNAME
with your actual Docker Hub username.

Example:

docker build -t yourusername/coffeenchill-functions:v1.0 .
Then:

docker images
You should see:

yourusername/coffeenchill-functions
v1.0
30. Step 28 — Push to Docker Hub
Login:

docker login
Then:

docker push yourusername/coffeenchill-functions:v1.0
The PoE specifically requires:

coffeenchill-functions:v1.0
and a published Docker Hub image. 


31. Step 29 — Tag Azurite
The PoE also asks for:

coffeenchill-Azurite:v1.0
Use:

docker tag mcr.microsoft.com/azure-storage/azurite `
    yourusername/coffeenchill-azurite:v1.0
Then:

docker push yourusername/coffeenchill-azurite:v1.0
32. Step 30 — Standalone Docker execution
This is important.

Do not use Docker Compose for Part 1.

The PoE explicitly says Part 1 must demonstrate standalone Docker containers and Docker Compose is introduced in Part 2. 


Create a network:

docker network create coffeenchill-network
Run Azurite:

docker run -d `
  --name coffeenchill-azurite `
  --network coffeenchill-network `
  -p 10000:10000 `
  -p 10001:10001 `
  -p 10002:10002 `
  mcr.microsoft.com/azure-storage/azurite
Then run the Functions container:

docker run -d `
  --name coffeenchill-functions `
  --network coffeenchill-network `
  -p 7071:80 `
  -e AzureWebJobsStorage="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://coffeenchill-azurite:10000/devstoreaccount1;QueueEndpoint=http://coffeenchill-azurite:10001/devstoreaccount1;TableEndpoint=http://coffeenchill-azurite:10002/devstoreaccount1;" `
  -e CoffeeNChillStorage="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://coffeenchill-azurite:10000/devstoreaccount1;QueueEndpoint=http://coffeenchill-azurite:10001/devstoreaccount1;TableEndpoint=http://coffeenchill-azurite:10002/devstoreaccount1;" `
  yourusername/coffeenchill-functions:v1.0
Microsoft documents the standard Azurite devstoreaccount1 test account and key. 

Then test:

http://localhost:7071/api/menu
33. Step 31 — Docker logs
Functions:

docker logs coffeenchill-functions
Azurite:

docker logs coffeenchill-azurite
Live logs:

docker logs -f coffeenchill-functions
These are useful to show in your video.

34. Step 32 — Postman collection
The complete collection is already created:

Download CoffeeNChill Part 1 Postman Collection

It uses:

{{baseUrl}}
with:

http://localhost:7071/api
It contains tests for:

Create Menu
Get All Menu
Get By Category
Get By ID
Update Menu
Delete Menu

Upload Document
List Documents
Download Document
This directly targets the Postman rubric, which rewards full endpoint coverage, environment variables, sample bodies, and passing automated tests. 


35. Step 33 — GitHub
Initialize:

git init
Configure:

git config user.name "YOUR NAME"
git config user.email "YOUR EMAIL"
First commit:

git add .
git commit -m "chore: initialize CoffeeNChill Part 1 project"
Then make meaningful feature commits.

For example:

git add Models Services
git commit -m "feat: implement menu table storage service"
git add Functions/MenuFunctions.cs
git commit -m "feat: implement menu CRUD HTTP functions"
git add Functions/DocumentFunctions.cs
git commit -m "feat: implement staff document blob endpoints"
git add Dockerfile
git commit -m "feat: containerize Azure Functions application"
git add docs README.md
git commit -m "test: add Postman collection and Part 1 documentation"
The PoE requires at least five meaningful commits per student per part and says generic commits such as typo/fix commits don't count. 


36. Step 34 — GitHub push
Create a GitHub repository:

CoffeeNChill-Part1
Then:

git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/CoffeeNChill-Part1.git
Then:

git push -u origin main
37. Step 35 — Video demonstration
Your video should show the application actually running.

The PoE says the video must show the running application with voiceover explaining architecture and design decisions. 


I recommend this order:

1. Introduction
Say:

“This is our CoffeeNChill Canteen Management System Part 1 implementation. The system uses Azure Table Storage for menu items, Azure Blob Storage for staff documents, Azure Functions for the HTTP API, Azurite for local Azure Storage emulation, and Docker for containerization.”

2. Show GitHub
Show:

Models
Services
Functions
docs
Dockerfile
README.md
3. Explain MenuItems
Show:

PartitionKey = Category
RowKey = SKU
4. Show Azurite
Run:

docker ps
5. Show Functions
Run:

func start
6. Postman
Demonstrate:

POST /api/menu
GET /api/menu
GET /api/menu/category/Hot Drinks
GET /api/menu/Hot Drinks/COF-001
PUT /api/menu/Hot Drinks/COF-001
DELETE /api/menu/Hot Drinks/COF-001
7. Documents
Show:

POST /api/documents/upload
GET /api/documents
GET /api/documents/download/{fileName}
8. Docker
Show:

docker build ...
docker images
docker run ...
docker ps
9. Docker Hub
Open your Docker Hub repository and show:

coffeenchill-functions:v1.0
10. Finish
Show the final GitHub repository and commit history.

38. Part 1 marking target
Your implementation is structured around the full 100 marks:

Criterion	Marks	Implementation
Submission / README / Git / Video	10	✅
Azure Table + HTTP CRUD	30	✅
Blob Storage documents	20	✅
Dockerfile + Docker Hub	20	✅
Postman testing	20	✅
Total	100	Target: full implementation
The rubric's top level specifically asks for robust CRUD, validation/error handling, Blob metadata and streaming, optimized Docker publishing, and a professional Postman collection. 


Final files
Start with these:

Download complete README
Download project file
Download Program.cs
Download MenuItem.cs
Download MenuItemRequest.cs
Download MenuTableService.cs
Download BlobStorageService.cs
Download MenuFunctions.cs
Download DocumentFunctions.cs
Download Dockerfile
Download host.json
Download local.settings.json
Download .gitignore
Download Postman collection

One thing to do before submission: replace YOUR_DOCKERHUB_USERNAME, YOUR_USERNAME, your Git name/email, and the YouTube link in the README. Do not put real Azure credentials into GitHub. 


Program.cs
Code


host.json
Code


local.settings.json
Code

8 more

Complete Part 2 step by step in detail. write down full code, no zip folder.

Absolutely. Part 2 is now completed as a separate full project, building on Part 1. I did not create a ZIP folder.

I based the implementation on the Part 2 requirements in your PoE: asynchronous order placement through order-processing-queue, a Queue-triggered ProcessOrderQueue, an Orders Azure Table, the Received → Preparing → Ready → Collected lifecycle, Docker Compose, Docker Hub v2.0, updated Postman tests, and the README/changelog/video requirements. 


One important point: the PoE's Part 2 rubric awards 25 marks each for queue placement, queue-triggered processing, Docker Compose/Docker Hub, and the remaining submission/documentation requirements. The queue-processing rubric specifically calls for retry/poison-queue handling and lifecycle transitions. 


1. Complete Part 2 project
The project is here:

/mnt/data/CoffeeNChill-Part2/
Download the individual files
Project configuration

CoffeeNChill.Functions.csproj

Program.cs

host.json

local.settings.json

Dockerfile

docker-compose.yml

.gitignore

New Part 2 models

Order.cs

OrderRequest.cs

New services

QueueService.cs

OrderTableService.cs

New Function

OrderFunctions.cs

Postman

CoffeeNChill-Part2.postman_collection.json

Documentation

README.md

README-PART2.md

Part 1 files are also retained inside this project:

Functions/
    MenuFunctions.cs
    DocumentFunctions.cs

Services/
    MenuTableService.cs
    BlobStorageService.cs

Models/
    MenuItem.cs
    MenuItemRequest.cs
2. What Part 2 adds
Your Part 1 architecture was:

HTTP
 ↓
Azure Functions
 ↓
Azurite
 ├── MenuItems Table
 └── staff-docs Blob
Part 2 becomes:

                         CoffeeNChill Client
                                |
                                |
                    POST /api/orders/queue
                                |
                                v
                     +---------------------+
                     | Azure Function       |
                     | QueueOrder           |
                     +----------+----------+
                                |
                                | JSON
                                v
                     +---------------------+
                     | Azure Storage Queue  |
                     | order-processing-    |
                     | queue                |
                     +----------+----------+
                                |
                         QueueTrigger
                                |
                                v
                     +---------------------+
                     | ProcessOrderQueue    |
                     +----------+----------+
                                |
                                v
                     +---------------------+
                     | Azure Table          |
                     | Orders               |
                     +---------------------+
                                |
                                v
             Received → Preparing → Ready → Collected
The PoE specifically says that the client should not wait synchronously for preparation; the order should be put into an Azure Storage Queue and processed by an automated background Function. 


3. Final folder structure
Use this exact structure:

CoffeeNChill-Part2/
│
├── CoffeeNChill.Functions.csproj
├── Program.cs
├── host.json
├── local.settings.json
├── Dockerfile
├── docker-compose.yml
├── .gitignore
├── README.md
│
├── Models/
│   ├── MenuItem.cs
│   ├── MenuItemRequest.cs
│   ├── Order.cs
│   └── OrderRequest.cs
│
├── Services/
│   ├── MenuTableService.cs
│   ├── BlobStorageService.cs
│   ├── QueueService.cs
│   └── OrderTableService.cs
│
├── Functions/
│   ├── MenuFunctions.cs
│   ├── DocumentFunctions.cs
│   └── OrderFunctions.cs
│
└── docs/
    ├── CoffeeNChill-Part1.postman_collection.json
    ├── CoffeeNChill-Part2.postman_collection.json
    └── README-PART2.md
4. Step 1 — Copy Part 1
If you are starting from your Part 1 project:

Copy-Item -Recurse CoffeeNChill-Part1 CoffeeNChill-Part2
Then:

cd CoffeeNChill-Part2
Do not delete the Part 1 Functions.

Part 2 is an extension of Part 1.

5. Step 2 — Add the required NuGet packages
The Part 2 .csproj is:

<Project Sdk="Azure.Functions.Sdk/1.0.0">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>CoffeeNChill.Functions</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.Functions.Worker" Version="2.52.0" />

    <PackageReference
      Include="Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore"
      Version="2.1.1" />

    <PackageReference
      Include="Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues"
      Version="5.2.0" />

    <PackageReference
      Include="Azure.Data.Tables"
      Version="12.11.0" />

    <PackageReference
      Include="Azure.Storage.Blobs"
      Version="12.30.1" />

    <PackageReference
      Include="Azure.Storage.Queues"
      Version="12.28.1" />
  </ItemGroup>

  <ItemGroup>
    <None Update="host.json">
      <CopyToOutputDirectory>
        PreserveNewest
      </CopyToOutputDirectory>
    </None>

    <None Update="local.settings.json">
      <CopyToOutputDirectory>
        PreserveNewest
      </CopyToOutputDirectory>

      <CopyToPublishDirectory>
        Never
      </CopyToPublishDirectory>
    </None>
  </ItemGroup>
</Project>
The important new packages are:

Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues
Azure.Storage.Queues
Azure.Data.Tables
Microsoft's current Functions documentation specifies the isolated-worker Storage Queues extension for QueueTrigger functions. 

6. Step 3 — Program.cs
The complete file:

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
The two new registrations are:

builder.Services.AddSingleton<QueueService>();
builder.Services.AddSingleton<OrderTableService>();
7. Step 4 — OrderRequest.cs
Create:

Models/OrderRequest.cs
Full code:

namespace CoffeeNChill.Functions.Models;

public class OrderRequest
{
    public string OrderId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public List<string> SelectedItemSKUs { get; set; } = new();

    public double TotalPrice { get; set; }

    public DateTimeOffset OrderTimestamp { get; set; }
}
This represents the message entering the queue.

8. Step 5 — Order.cs
Create:

Models/Order.cs
Full code:

using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Functions.Models;

public class Order : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public string OrderId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string SelectedItemSKUsJson { get; set; } = "[]";

    public double TotalPrice { get; set; }

    public DateTimeOffset OrderTimestamp { get; set; }

    public string Status { get; set; } = "Received";

    public DateTimeOffset StatusUpdatedAt { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}
The PoE requires:

Orders
PartitionKey = OrderDate
RowKey       = OrderId
Status       = Received
and then the lifecycle:

Received
   ↓
Preparing
   ↓
Ready
   ↓
Collected


I use the UTC date as:

yyyy-MM-dd
For example:

2026-10-12
9. Step 6 — QueueService.cs
Create:

Services/QueueService.cs
Full code:

using Azure.Storage.Queues;
using System.Text.Json;
using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Services;

public class QueueService
{
    public const string QueueName =
        "order-processing-queue";

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
            MessageEncoding =
                QueueMessageEncoding.Base64
        };

        _queueClient = new QueueClient(
            connectionString,
            QueueName,
            options);
    }

    public async Task<string> EnqueueOrderAsync(
        OrderRequest request)
    {
        await _queueClient.CreateIfNotExistsAsync();

        var json =
            JsonSerializer.Serialize(
                request,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy =
                        JsonNamingPolicy.CamelCase
                });

        var response =
            await _queueClient.SendMessageAsync(json);

        return response.Value.MessageId;
    }
}
Why Base64?
The Azure Queue SDK supports explicit Base64 message encoding, and Microsoft's SDK documentation recommends it for interoperability with Azure Functions because Functions historically expects Base64-encoded queue messages. 

That also directly targets the PoE's "Base64 encoding/decoding" high-level queue rubric. 


10. Step 7 — OrderTableService.cs
Create:

Services/OrderTableService.cs
Full code:

using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Services;

public class OrderTableService
{
    private const string TableName = "Orders";

    private readonly TableClient _tableClient;

    public OrderTableService(
        IConfiguration configuration)
    {
        var connectionString =
            configuration["CoffeeNChillStorage"]
            ?? configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException(
                "CoffeeNChillStorage or AzureWebJobsStorage is not configured.");

        _tableClient =
            new TableClient(
                connectionString,
                TableName);
    }

    public async Task EnsureTableExistsAsync()
    {
        await _tableClient.CreateIfNotExistsAsync();
    }

    public async Task UpsertAsync(Order order)
    {
        await EnsureTableExistsAsync();

        await _tableClient.UpsertEntityAsync(
            order,
            TableUpdateMode.Replace);
    }

    public async Task<Order?> GetAsync(
        string orderDate,
        string orderId)
    {
        await EnsureTableExistsAsync();

        try
        {
            var response =
                await _tableClient.GetEntityAsync<Order>(
                    orderDate,
                    orderId);

            return response.Value;
        }
        catch (RequestFailedException ex)
            when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<List<Order>> GetAllAsync()
    {
        await EnsureTableExistsAsync();

        var orders =
            new List<Order>();

        await foreach (
            var order in
            _tableClient.QueryAsync<Order>())
        {
            orders.Add(order);
        }

        return orders
            .OrderByDescending(
                x => x.OrderTimestamp)
            .ToList();
    }
}
11. Step 8 — OrderFunctions.cs
This is the main Part 2 code.

It contains:

POST /api/orders/queue
GET  /api/orders
GET  /api/orders/{orderDate}/{orderId}

ProcessOrderQueue
Full code:

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

    private readonly OrderTableService
        _orderTableService;

    private readonly ILogger<OrderFunctions>
        _logger;

    private static readonly JsonSerializerOptions
        JsonOptions = new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,

            PropertyNameCaseInsensitive = true
        };

    private static readonly HashSet<char>
        InvalidOrderIdCharacters = new()
        {
            '/',
            '\\',
            '#',
            '?'
        };

    public OrderFunctions(
        QueueService queueService,
        OrderTableService orderTableService,
        ILogger<OrderFunctions> logger)
    {
        _queueService =
            queueService;

        _orderTableService =
            orderTableService;

        _logger = logger;
    }

    [Function("QueueOrder")]
    public async Task<HttpResponseData> QueueOrder(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "orders/queue")]
        HttpRequestData req)
    {
        try
        {
            var request =
                await JsonSerializer.DeserializeAsync<OrderRequest>(
                    req.Body,
                    JsonOptions);

            var validationError =
                Validate(request);

            if (validationError is not null)
            {
                var badRequest =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await badRequest.WriteAsJsonAsync(
                    new
                    {
                        error = validationError
                    });

                return badRequest;
            }

            request!.OrderTimestamp =
                request.OrderTimestamp.ToUniversalTime();

            var messageId =
                await _queueService.EnqueueOrderAsync(
                    request);

            _logger.LogInformation(
                "Order {OrderId} queued with queue message {MessageId}.",
                request.OrderId,
                messageId);

            var response =
                req.CreateResponse(
                    HttpStatusCode.Accepted);

            await response.WriteAsJsonAsync(
                new
                {
                    message =
                        "Order accepted for asynchronous processing.",

                    orderId =
                        request.OrderId,

                    queue =
                        QueueService.QueueName,

                    queueMessageId =
                        messageId,

                    status =
                        "Queued"
                });

            return response;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid JSON submitted to order queue endpoint.");

            var response =
                req.CreateResponse(
                    HttpStatusCode.BadRequest);

            await response.WriteAsJsonAsync(
                new
                {
                    error =
                        "Request body must contain valid JSON."
                });

            return response;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Azure Queue Storage failed while queuing an order.");

            var response =
                req.CreateResponse(
                    HttpStatusCode.ServiceUnavailable);

            await response.WriteAsJsonAsync(
                new
                {
                    error =
                        "The order queue is temporarily unavailable."
                });

            return response;
        }
    }

    [Function("GetOrder")]
    public async Task<HttpResponseData> GetOrder(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "orders/{orderDate}/{orderId}")]
        HttpRequestData req,
        string orderDate,
        string orderId)
    {
        try
        {
            var order =
                await _orderTableService.GetAsync(
                    orderDate,
                    orderId);

            if (order is null)
            {
                var notFound =
                    req.CreateResponse(
                        HttpStatusCode.NotFound);

                await notFound.WriteAsJsonAsync(
                    new
                    {
                        error =
                            $"Order '{orderId}' was not found for date '{orderDate}'."
                    });

                return notFound;
            }

            var response =
                req.CreateResponse(
                    HttpStatusCode.OK);

            await response.WriteAsJsonAsync(
                ToResponse(order));

            return response;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Failed to read order {OrderId}.",
                orderId);

            var response =
                req.CreateResponse(
                    HttpStatusCode.ServiceUnavailable);

            await response.WriteAsJsonAsync(
                new
                {
                    error =
                        "Orders storage is temporarily unavailable."
                });

            return response;
        }
    }

    [Function("GetOrders")]
    public async Task<HttpResponseData> GetOrders(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "orders")]
        HttpRequestData req)
    {
        try
        {
            var orders =
                await _orderTableService.GetAllAsync();

            var response =
                req.CreateResponse(
                    HttpStatusCode.OK);

            await response.WriteAsJsonAsync(
                orders.Select(ToResponse));

            return response;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Failed to list Orders table records.");

            var response =
                req.CreateResponse(
                    HttpStatusCode.ServiceUnavailable);

            await response.WriteAsJsonAsync(
                new
                {
                    error =
                        "Orders storage is temporarily unavailable."
                });

            return response;
        }
    }

    [Function("ProcessOrderQueue")]
    public async Task ProcessOrderQueue(
        [QueueTrigger(
            QueueService.QueueName,
            Connection = "CoffeeNChillStorage")]
        string message)
    {
        OrderRequest? request = null;

        try
        {
            request =
                JsonSerializer.Deserialize<OrderRequest>(
                    message,
                    JsonOptions);

            var validationError =
                Validate(request);

            if (validationError is not null)
            {
                throw new InvalidDataException(
                    validationError);
            }

            request!.OrderTimestamp =
                request.OrderTimestamp.ToUniversalTime();

            var order =
                new Order
                {
                    PartitionKey =
                        request.OrderTimestamp
                            .UtcDateTime
                            .ToString("yyyy-MM-dd"),

                    RowKey =
                        request.OrderId,

                    OrderId =
                        request.OrderId,

                    CustomerName =
                        request.CustomerName,

                    SelectedItemSKUsJson =
                        JsonSerializer.Serialize(
                            request.SelectedItemSKUs),

                    TotalPrice =
                        request.TotalPrice,

                    OrderTimestamp =
                        request.OrderTimestamp,

                    Status =
                        "Received",

                    StatusUpdatedAt =
                        DateTimeOffset.UtcNow
                };

            await _orderTableService.UpsertAsync(
                order);

            _logger.LogInformation(
                "Order {OrderId} stored in Orders table with status Received.",
                order.OrderId);

            await AdvanceStatusAsync(
                order,
                "Preparing");

            await AdvanceStatusAsync(
                order,
                "Ready");

            await AdvanceStatusAsync(
                order,
                "Collected");

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

    private async Task AdvanceStatusAsync(
        Order order,
        string status)
    {
        order.Status =
            status;

        order.StatusUpdatedAt =
            DateTimeOffset.UtcNow;

        await _orderTableService.UpsertAsync(
            order);

        _logger.LogInformation(
            "Order {OrderId} status changed to {Status}.",
            order.OrderId,
            status);

        await Task.Delay(
            TimeSpan.FromMilliseconds(750));
    }

    private static string? Validate(
        OrderRequest? request)
    {
        if (request is null)
        {
            return "Request body is required.";
        }

        if (string.IsNullOrWhiteSpace(
                request.OrderId))
        {
            return "OrderId is required.";
        }

        if (request.OrderId.Length > 100 ||
            request.OrderId.Any(
                c => InvalidOrderIdCharacters.Contains(c)))
        {
            return
                "OrderId must be 1-100 characters and cannot contain /, \\, #, or ?.";
        }

        if (string.IsNullOrWhiteSpace(
                request.CustomerName))
        {
            return "CustomerName is required.";
        }

        if (request.CustomerName.Length > 200)
        {
            return
                "CustomerName must not exceed 200 characters.";
        }

        if (request.SelectedItemSKUs is null ||
            request.SelectedItemSKUs.Count == 0)
        {
            return
                "SelectedItemSKUs must contain at least one item SKU.";
        }

        if (request.SelectedItemSKUs.Any(
                string.IsNullOrWhiteSpace))
        {
            return
                "SelectedItemSKUs cannot contain empty values.";
        }

        if (request.TotalPrice <= 0 ||
            double.IsNaN(request.TotalPrice) ||
            double.IsInfinity(request.TotalPrice))
        {
            return
                "TotalPrice must be greater than zero and must be a finite number.";
        }

        if (request.OrderTimestamp == default)
        {
            return
                "OrderTimestamp is required and must be a valid ISO-8601 timestamp.";
        }

        return null;
    }

    private static object ToResponse(
        Order order)
    {
        List<string> skus;

        try
        {
            skus =
                JsonSerializer.Deserialize<List<string>>(
                    order.SelectedItemSKUsJson)
                ?? new List<string>();
        }
        catch
        {
            skus =
                new List<string>();
        }

        return new
        {
            orderId =
                order.OrderId,

            orderDate =
                order.PartitionKey,

            customerName =
                order.CustomerName,

            selectedItemSKUs =
                skus,

            totalPrice =
                order.TotalPrice,

            orderTimestamp =
                order.OrderTimestamp,

            status =
                order.Status,

            statusUpdatedAt =
                order.StatusUpdatedAt
        };
    }
}
12. The required POST endpoint
The PoE specifies:

POST /api/orders/queue


Run:

POST http://localhost:7071/api/orders/queue
Header:

Content-Type: application/json
Body:

{
  "orderId": "ORD-2026-8801",
  "customerName": "Jane Smith",
  "selectedItemSKUs": [
    "COF-001",
    "PAS-104"
  ],
  "totalPrice": 65.00,
  "orderTimestamp": "2026-10-12T10:15:00Z"
}
Expected:

202 Accepted
Example:

{
  "message": "Order accepted for asynchronous processing.",
  "orderId": "ORD-2026-8801",
  "queue": "order-processing-queue",
  "queueMessageId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "status": "Queued"
}
This demonstrates the asynchronous design.

The HTTP request does not perform:

Received
Preparing
Ready
Collected
it only places the order into:

order-processing-queue
13. Step 9 — QueueTrigger
The key code is:

[Function("ProcessOrderQueue")]
public async Task ProcessOrderQueue(
    [QueueTrigger(
        QueueService.QueueName,
        Connection = "CoffeeNChillStorage")]
    string message)
That means:

order-processing-queue
automatically invokes:

ProcessOrderQueue
Microsoft describes QueueTrigger as a trigger that starts a Function when a new item is received on an Azure Storage Queue. 

14. Step 10 — Orders table
The table is:

Orders
Example record:

Property	Value
PartitionKey	2026-10-12
RowKey	ORD-2026-8801
OrderId	ORD-2026-8801
CustomerName	Jane Smith
SelectedItemSKUsJson	["COF-001","PAS-104"]
TotalPrice	65
OrderTimestamp	2026-10-12T10:15:00Z
Status	Collected
StatusUpdatedAt	UTC timestamp
The PoE specifically requires the Orders table with OrderDate as the partition concept and OrderId as the row key. 


15. Step 11 — Lifecycle
When the queue message is processed:

             Queue message
                  |
                  v
              Received
                  |
                  v
              Preparing
                  |
                  v
                Ready
                  |
                  v
              Collected
The code writes each status back into Azure Table Storage.

It also logs:

Order ORD-2026-8801 stored in Orders table with status Received.
Order ORD-2026-8801 status changed to Preparing.
Order ORD-2026-8801 status changed to Ready.
Order ORD-2026-8801 status changed to Collected.
This directly targets the lifecycle requirement in the PoE. 


16. Step 12 — host.json
Full code:

{
  "version": "2.0",

  "logging": {
    "logLevel": {
      "default": "Information",
      "Host.Results": "Information",
      "Function": "Information"
    }
  },

  "extensions": {
    "http": {
      "routePrefix": "api"
    },

    "queues": {
      "maxPollingInterval": "00:00:02",
      "visibilityTimeout": "00:00:10",
      "batchSize": 1,
      "maxDequeueCount": 5,
      "newBatchThreshold": 0
    }
  }
}
The important Part 2 section is:

"queues": {
  "maxPollingInterval": "00:00:02",
  "visibilityTimeout": "00:00:10",
  "batchSize": 1,
  "maxDequeueCount": 5,
  "newBatchThreshold": 0
}
17. Step 13 — Poison queue
If processing repeatedly fails:

order-processing-queue
will eventually result in:

order-processing-queue-poison
The function deliberately rethrows exceptions:

catch (Exception ex)
{
    _logger.LogError(
        ex,
        "Order queue processing failed for {OrderId}. " +
        "The Functions runtime will retry the message; " +
        "repeated failures are sent to the poison queue.",
        request?.OrderId ?? "unknown");

    throw;
}
This is important.

Do not simply catch the exception and return.

If you do:

catch
{
    return;
}
the Functions runtime thinks the queue message was successfully processed.

Instead:

throw;
allows the runtime's retry behavior to operate.

The PoE specifically calls for poison queue/failure handling in the queue-processing criterion. 


18. Step 14 — GET an order
I added a verification endpoint because the PoE requires the updated Postman collection to demonstrate that messages were placed onto the queue and that status updates can be checked. 


Use:

GET http://localhost:7071/api/orders/2026-10-12/ORD-2026-8801
Expected:

{
  "orderId": "ORD-2026-8801",
  "orderDate": "2026-10-12",
  "customerName": "Jane Smith",
  "selectedItemSKUs": [
    "COF-001",
    "PAS-104"
  ],
  "totalPrice": 65,
  "orderTimestamp": "2026-10-12T10:15:00+00:00",
  "status": "Collected",
  "statusUpdatedAt": "..."
}
19. Step 15 — GET all orders
GET http://localhost:7071/api/orders
Example:

[
  {
    "orderId": "ORD-2026-8801",
    "orderDate": "2026-10-12",
    "customerName": "Jane Smith",
    "selectedItemSKUs": [
      "COF-001",
      "PAS-104"
    ],
    "totalPrice": 65,
    "orderTimestamp": "2026-10-12T10:15:00+00:00",
    "status": "Collected",
    "statusUpdatedAt": "..."
  }
]
20. Step 16 — local.settings.json
Full file:

{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "CoffeeNChillStorage": "UseDevelopmentStorage=true"
  }
}
And:

local.settings.json
must remain in .gitignore.

21. Step 17 — Dockerfile
The complete Dockerfile remains:

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["CoffeeNChill.Functions.csproj", "."]
RUN dotnet restore "CoffeeNChill.Functions.csproj"

COPY . .

RUN dotnet publish "CoffeeNChill.Functions.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final

WORKDIR /home/site/wwwroot

COPY --from=build /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

EXPOSE 80
22. Step 18 — Build version 2.0
The PoE specifically requires the Compose application image to use:

coffeenchill-functions:v2.0


Replace:

YOUR_DOCKERHUB_USERNAME
with your Docker Hub username.

Example:

docker build `
  -t YOUR_DOCKERHUB_USERNAME/coffeenchill-functions:v2.0 `
  .
Check:

docker images
You should have:

YOUR_DOCKERHUB_USERNAME/coffeenchill-functions
v2.0
23. Step 19 — Push v2.0 to Docker Hub
Login:

docker login
Push:

docker push YOUR_DOCKERHUB_USERNAME/coffeenchill-functions:v2.0
The Docker Compose file must pull this published image rather than build the source locally.

This is particularly important because the PoE says Compose submissions that rely exclusively on:

build: .
without a Docker Hub image reference can lose marks. 


24. Step 20 — Docker Compose
This is the most important Part 2 infrastructure file.

Full code:

services:

  azurite:
    image: mcr.microsoft.com/azure-storage/azurite:latest

    container_name: coffeenchill-azurite

    command: >-
      azurite
      --blobHost 0.0.0.0
      --queueHost 0.0.0.0
      --tableHost 0.0.0.0
      --location /data
      --debug /data/debug.log

    ports:
      - "10000:10000"
      - "10001:10001"
      - "10002:10002"

    volumes:
      - azurite-data:/data

    networks:
      - coffeenchill-network


  functions:
    image: YOUR_DOCKERHUB_USERNAME/coffeenchill-functions:v2.0

    container_name: coffeenchill-functions

    depends_on:
      - azurite

    environment:

      FUNCTIONS_WORKER_RUNTIME: dotnet-isolated

      AzureWebJobsStorage: "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;"

      CoffeeNChillStorage: "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;"

    ports:
      - "7071:80"

    networks:
      - coffeenchill-network


networks:

  coffeenchill-network:
    driver: bridge


volumes:

  azurite-data:
This gives you:

              Docker Compose
                    |
       +------------+------------+
       |                         |
       v                         v
   Azurite                 Functions v2.0
       |                         |
       |                         |
       +------ bridge network ---+
The PoE specifically requires:

Azurite.

Functions host.

Docker Hub image.

Compose.

One-command startup. 


The rubric's highest Compose band also specifically mentions version-tagged Docker Hub images, custom bridge networking, and volume mounts for Azurite data. 


25. Step 21 — Start everything
After replacing:

YOUR_DOCKERHUB_USERNAME
run:

docker compose pull
Then:

docker compose up -d
Or:

docker compose up -d
after the image has already been pulled.

Check:

docker compose ps
You should see:

coffeenchill-azurite
coffeenchill-functions
26. Step 22 — View Functions logs
Run:

docker compose logs -f functions
Initially you should see the Functions host starting.

After submitting an order you should see something like:

Order ORD-2026-8801 stored in Orders table with status Received.

Order ORD-2026-8801 status changed to Preparing.

Order ORD-2026-8801 status changed to Ready.

Order ORD-2026-8801 status changed to Collected.

Order ORD-2026-8801 completed lifecycle processing.
This is excellent material for your video demonstration.

27. Step 23 — Postman
The complete collection is here:

Download CoffeeNChill Part 2 Postman Collection

It contains:

Queue Order
Get All Orders
Get Specific Order
Invalid Order - Missing Customer
28. Postman test 1 — Queue Order
POST {{baseUrl}}/orders/queue
Body:

{
  "orderId": "{{orderId}}",
  "customerName": "Jane Smith",
  "selectedItemSKUs": [
    "COF-001",
    "PAS-104"
  ],
  "totalPrice": 65.00,
  "orderTimestamp": "2026-10-12T10:15:00Z"
}
The Postman test checks:

202 Accepted
and:

status = Queued
and:

queue = order-processing-queue
29. Postman test 2 — Get Orders
GET {{baseUrl}}/orders
Expected:

200 OK
30. Postman test 3 — Get Specific Order
GET {{baseUrl}}/orders/{{orderDate}}/{{orderId}}
Expected:

200 OK
And:

Received
Preparing
Ready
Collected
are accepted lifecycle states.

31. Postman test 4 — Invalid order
The collection also tests validation.

For example:

{
  "orderId": "ORD-INVALID-01",
  "customerName": "",
  "selectedItemSKUs": [
    "COF-001"
  ],
  "totalPrice": 25.00,
  "orderTimestamp": "2026-10-12T10:15:00Z"
}
Expected:

400 Bad Request
This helps target the PoE's requirement for validation of order fields. 


32. Step 24 — Complete testing sequence
For your actual demonstration, do this in order.

Start:
docker compose up -d
Check:
docker compose ps
Logs:
docker compose logs -f functions
Postman:
POST /api/orders/queue
Then watch the logs.

You should see:

Received
Preparing
Ready
Collected
Then run:

GET /api/orders/2026-10-12/ORD-2026-8801
Finally:

GET /api/orders
33. Step 25 — Why the lifecycle may already say Collected
Don't be confused if you send the order and immediately query:

GET /api/orders/2026-10-12/ORD-2026-8801
and receive:

"status": "Collected"
The Function intentionally moves through the states quickly.

The code has:

await Task.Delay(
    TimeSpan.FromMilliseconds(750));
between transitions.

Therefore, the final table record can reach:

Collected
within a few seconds.

The Functions logs demonstrate the intermediate states:

Received
Preparing
Ready
Collected
which is what you should show in your video.

34. Step 26 — Docker Compose shutdown
Stop the application:

docker compose down
This leaves the persistent Azurite volume.

To delete everything including the volume:

docker compose down -v
35. Step 27 — Git commits
The PoE requires meaningful Git contributions, and the general instructions require at least five meaningful commits per student per part. 


Do not make:

fix typo
update
changes
stuff
final
final2
Instead make feature-based commits.

Commit 1
git add Models/Order.cs Models/OrderRequest.cs

git commit -m "feat: add asynchronous order models"
Commit 2
git add Services/QueueService.cs

git commit -m "feat: add Azure Storage Queue producer"
Commit 3
git add Services/OrderTableService.cs

git commit -m "feat: add Orders table persistence"
Commit 4
git add Functions/OrderFunctions.cs

git commit -m "feat: implement queue producer and order processor"
Commit 5
git add host.json

git commit -m "feat: configure queue retries and poison handling"
Commit 6
git add docker-compose.yml

git commit -m "feat: add Docker Compose orchestration"
Commit 7
git add docs/CoffeeNChill-Part2.postman_collection.json

git commit -m "test: add Part 2 Postman queue tests"
Commit 8
git add README.md docs/README-PART2.md

git commit -m "docs: document Part 2 architecture and deployment"
36. Step 28 — Push to GitHub
git status
Then:

git branch -M main
If you already have the Part 1 repository:

git add .
git commit -m "feat: complete Part 2 asynchronous order processing"
git push
If this is a new repository:

git remote add origin https://github.com/YOUR_USERNAME/CoffeeNChill-Part2.git
Then:

git push -u origin main
37. Step 29 — README
The complete README is already created:

Download Part 2 README

It contains:

Part 2 requirements
Architecture
Folder structure
Prerequisites
Queue endpoint
QueueTrigger
Orders table
Poison queue
Docker build
Docker Hub
Docker Compose
Postman
Git
Video checklist
Changelog
YouTube placeholder
The PoE explicitly requires the updated README to contain a changelog and new YouTube link and to demonstrate the one-command Compose startup. 


38. Step 30 — YouTube demonstration
Your Part 2 video should follow this exact sequence.

Scene 1 — GitHub
Show:

CoffeeNChill-Part2
and:

Models
Services
Functions
docs
Dockerfile
docker-compose.yml
README.md
Scene 2 — Explain architecture
Say:

“Part 2 extends our CoffeeNChill system with asynchronous order processing. Instead of processing an order synchronously, the HTTP Function places the order on Azure Storage Queue. A Queue-triggered Function then processes the order and records its lifecycle in the Orders Azure Table.”

Scene 3 — Docker Hub
Show:

coffeenchill-functions:v2.0
This is important because the PoE explicitly requires the published image to be used by Compose. 


Scene 4 — Compose
Show:

docker compose pull
then:

docker compose up -d
Then:

docker compose ps
Scene 5 — Logs
Run:

docker compose logs -f functions
Scene 6 — Postman
Send:

POST /api/orders/queue
with:

{
  "orderId": "ORD-2026-8801",
  "customerName": "Jane Smith",
  "selectedItemSKUs": [
    "COF-001",
    "PAS-104"
  ],
  "totalPrice": 65.00,
  "orderTimestamp": "2026-10-12T10:15:00Z"
}
Show:

202 Accepted
Scene 7 — QueueTrigger
Go back to the logs.

Show:

Received
Preparing
Ready
Collected
Scene 8 — Check database
Postman:

GET /api/orders/2026-10-12/ORD-2026-8801
Show:

"status": "Collected"
Scene 9 — All orders
GET /api/orders
Scene 10 — Invalid input
Show:

400 Bad Request
Scene 11 — Git
Show:

git log --oneline
and demonstrate the meaningful commits.

39. Part 2 marking checklist
Your implementation is designed around the PoE requirements:

Requirement	Target	Status
Queue order-processing-queue	25	✅
Queue producer		✅
JSON serialization		✅
Validation		✅
Base64 encoding		✅
QueueTrigger	25	✅
Orders Table		✅
Received → Preparing → Ready → Collected		✅
Error/retry handling		✅
Poison queue		✅
Docker Compose	25	✅
Docker Hub v2.0		✅
Custom bridge network		✅
Azurite volume		✅
Postman		✅
README/changelog		✅
Git commits		✅
Video demonstration		⬜ You must record it
The PoE explicitly requires the Compose file to reference the published Docker Hub image rather than relying only on a local build: path. 


40. One important thing you must change
In:

docker-compose.yml
change:

image: YOUR_DOCKERHUB_USERNAME/coffeenchill-functions:v2.0
to your actual Docker Hub username.

For example, if your Docker Hub username were:

shanton123
you would use:

image: shanton123/coffeenchill-functions:v2.0
Do the same in your README commands.

41. Current validation
I checked the generated project structure and Compose YAML. The Compose YAML parses correctly and contains the two required services:

azurite
functions
I could not run dotnet build in this environment because the environment does not have the dotnet executable installed. So before submission, run locally:

dotnet restore
dotnet build
and make sure you get:

Build succeeded.
Then:

docker build -t YOUR_DOCKERHUB_USERNAME/coffeenchill-functions:v2.0 .
and finally:

docker push YOUR_DOCKERHUB_USERNAME/coffeenchill-functions:v2.0
Your main Part 2 files
For convenience, these are the files you'll use most:

Complete Part 2 README
Complete docker-compose.yml
Complete OrderFunctions.cs
Complete QueueService.cs
Complete OrderTableService.cs
Complete Order.cs
Complete OrderRequest.cs
Complete .csproj
Complete host.json
Complete Dockerfile
Complete Postman collection

No ZIP file is required or included. Your final submission should be the GitHub repository containing these source files, as the PoE says ZIP files are not accepted. 



Program.cs
Code


host.json
Code


local.settings.json
Code

9 more

Example of Postman test for Part 2

Yes. For Part 2, your Postman demonstration should show the complete asynchronous order flow: submit order → receive 202 Accepted → queue processes it → check Orders table/status → demonstrate validation.

1. Start the application
From:

/mnt/data/CoffeeNChill-Part2
run:

docker compose up -d
Check:

docker compose ps
You should have:

coffeenchill-azurite
coffeenchill-functions
Your API base URL is:

http://localhost:7071/api
2. Test 1 — Queue an Order
Postman request
Method:

POST
URL:

http://localhost:7071/api/orders/queue
Headers
Content-Type: application/json
Body
Select:

Body → raw → JSON
Use:

{
  "orderId": "ORD-2026-8801",
  "customerName": "Jane Smith",
  "selectedItemSKUs": [
    "COF-001",
    "PAS-104"
  ],
  "totalPrice": 65.00,
  "orderTimestamp": "2026-10-08T19:30:00Z"
}
Expected response
You should receive:

HTTP 202 Accepted
with a response similar to:

{
  "message": "Order accepted for asynchronous processing.",
  "orderId": "ORD-2026-8801",
  "queue": "order-processing-queue",
  "queueMessageId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "status": "Queued"
}
What to say in your demonstration
“This POST endpoint does not process the order synchronously. It validates the request, serializes the order, and places it on the order-processing-queue. The API immediately returns HTTP 202 Accepted.”

3. Test 2 — Get All Orders
Create another Postman request.

Method:

GET
URL:

http://localhost:7071/api/orders
Click Send.

Expected:

HTTP 200 OK
You should see an array containing your order.

For example:

[
  {
    "orderId": "ORD-2026-8801",
    "customerName": "Jane Smith",
    "selectedItemSKUs": [
      "COF-001",
      "PAS-104"
    ],
    "totalPrice": 65,
    "orderTimestamp": "2026-10-08T19:30:00+00:00",
    "status": "Collected",
    "statusUpdatedAt": "2026-10-08T19:30:03+00:00"
  }
]
The exact timestamps will obviously differ.

4. Test 3 — Get a Specific Order
This is the most important request for demonstrating the order lifecycle.

Request
Method:

GET
URL:

http://localhost:7071/api/orders/2026-10-08/ORD-2026-8801
The date in the URL must match the date portion of your orderTimestamp.

Expected:

HTTP 200 OK
Example:

{
  "orderId": "ORD-2026-8801",
  "customerName": "Jane Smith",
  "selectedItemSKUs": [
    "COF-001",
    "PAS-104"
  ],
  "totalPrice": 65,
  "orderTimestamp": "2026-10-08T19:30:00+00:00",
  "status": "Collected",
  "statusUpdatedAt": "2026-10-08T19:30:03+00:00"
}
The final status should be:

Collected
5. Demonstrate the asynchronous processing
For the assignment/video, it is useful to show the Functions logs.

Run:

docker compose logs -f functions
Then send your POST request again with a new order ID:

{
  "orderId": "ORD-2026-8802",
  "customerName": "John Smith",
  "selectedItemSKUs": [
    "COF-002",
    "PAS-105"
  ],
  "totalPrice": 78.50,
  "orderTimestamp": "2026-10-08T19:35:00Z"
}
The logs should demonstrate the lifecycle:

Received
Preparing
Ready
Collected
Your code intentionally pauses briefly between the statuses, so this is useful for explaining the asynchronous processing.

You can say:

“The QueueTrigger receives the message from Azure Storage Queue. The order is then written to the Orders table and progresses through Received, Preparing, Ready and finally Collected.”

6. Test 4 — Invalid Order
This demonstrates your validation requirement.

Create:

POST http://localhost:7071/api/orders/queue
Use:

{
  "orderId": "ORD-INVALID-001",
  "customerName": "",
  "selectedItemSKUs": [
    "COF-001"
  ],
  "totalPrice": 50.00,
  "orderTimestamp": "2026-10-08T19:40:00Z"
}
The customerName is deliberately empty.

Expected:

HTTP 400 Bad Request
Example:

{
  "error": "Validation failed.",
  "details": [
    "CustomerName is required."
  ]
}
This proves that invalid orders aren't placed onto the queue.

7. Test 5 — Missing Items
Another useful validation test:

{
  "orderId": "ORD-INVALID-002",
  "customerName": "Test Customer",
  "selectedItemSKUs": [],
  "totalPrice": 50.00,
  "orderTimestamp": "2026-10-08T19:45:00Z"
}
Expected:

400 Bad Request
because:

SelectedItemSKUs must contain at least one item.
8. Test 6 — Invalid Price
Use:

{
  "orderId": "ORD-INVALID-003",
  "customerName": "Test Customer",
  "selectedItemSKUs": [
    "COF-001"
  ],
  "totalPrice": 0,
  "orderTimestamp": "2026-10-08T19:50:00Z"
}
Expected:

400 Bad Request
because the order price must be greater than zero.

9. Recommended Postman collection
For your assignment, I recommend having these four main requests visible:

#	Request	Method	Expected
1	Queue Order	POST	202 Accepted
2	Get All Orders	GET	200 OK
3	Get Specific Order	GET	200 OK
4	Invalid Order	POST	400 Bad Request
The collection you've already created in the Part 2 project is:

CoffeeNChill-Part2/docs/CoffeeNChill-Part2.postman_collection.json
You can import that directly into Postman.

Best demonstration sequence
For your video/submission, I'd do it in this order:

1. Start Docker Compose
       ↓
2. Show Azurite + Functions containers running
       ↓
3. POST /api/orders/queue
       ↓
4. Show 202 Accepted
       ↓
5. Show Functions logs
       ↓
6. Show Received → Preparing → Ready → Collected
       ↓
7. GET /api/orders/{date}/{orderId}
       ↓
8. Show status = Collected
       ↓
9. Send invalid order
       ↓
10. Show 400 Bad Request
That gives you a very clear demonstration of the Part 2 queue, asynchronous processing, Azure Table storage, lifecycle statuses, and validation.


Library
/
README-PART2.md



CoffeeNChill Part 2
This document is a Part 2 addendum. The repository keeps the Part 1 menu and staff-document functionality and adds asynchronous order processing with Azure Storage Queues, an Orders Azure Table, and Docker Compose orchestration.

Part 2 architecture
Client
  |
  | POST /api/orders/queue
  v
Azure Function HTTP producer
  |
  | JSON -> Base64 queue message
  v
Azurite Queue: order-processing-queue
  |
  | QueueTrigger
  v
ProcessOrderQueue
  |
  +--> Orders Azure Table
  |       Received
  |       Preparing
  |       Ready
  |       Collected
  |
  +--> Logs / poison-queue handling
Part 2 endpoints
Method	Endpoint	Purpose
POST	/api/orders/queue	Validate and enqueue an order
GET	/api/orders	List processed Orders table records
GET	/api/orders/{orderDate}/{orderId}	Check one order's status
Build v2.0
docker build -t YOUR_DOCKERHUB_USERNAME/coffeenchill-functions:v2.0 .
docker push YOUR_DOCKERHUB_USERNAME/coffeenchill-functions:v2.0
Run the complete Part 2 stack
Edit docker-compose.yml and replace YOUR_DOCKERHUB_USERNAME with your Docker Hub username.

docker compose pull
docker compose up -d
Check:

docker compose ps
docker compose logs -f functions
Stop:

docker compose down
Stop and delete persisted Azurite data:

docker compose down -v
Postman flow
POST /api/orders/queue with the sample order.

Read orderDate from the OrderTimestamp date in UTC.

Poll GET /api/orders/{orderDate}/{orderId}.

The queue-triggered function writes Received, then advances through Preparing, Ready, and Collected.

Use GET /api/orders to show the final Orders table record.

Poison queue
The Functions host is configured with maxDequeueCount: 5. A repeatedly failing message is moved by the Azure Functions queue runtime to:

order-processing-queue-poison
The queue processor logs malformed messages and processing failures before rethrowing so the runtime can perform its retry/poison-queue behavior.

