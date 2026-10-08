using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace CoffeeNChill.Functions.Services;

public class BlobStorageService
{
    private const string ContainerName = "staff-docs";

    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString =
            configuration["CoffeeNChillStorage"]
            ?? configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException(
                "CoffeeNChillStorage or AzureWebJobsStorage is not configured.");

        var serviceClient = new BlobServiceClient(connectionString);

        _containerClient = serviceClient.GetBlobContainerClient(ContainerName);
    }

    public async Task EnsureContainerExistsAsync()
    {
        await _containerClient.CreateIfNotExistsAsync(
            publicAccessType: PublicAccessType.None);
    }

    public async Task UploadAsync(
        string fileName,
        Stream content,
        string contentType)
    {
        await EnsureContainerExistsAsync();

        var blobClient = _containerClient.GetBlobClient(fileName);

        var headers = new BlobHttpHeaders
        {
            ContentType = string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType
        };

        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = headers
            });
    }

    public async Task<List<BlobDocumentInfo>> ListAsync()
    {
        await EnsureContainerExistsAsync();

        var documents = new List<BlobDocumentInfo>();

        await foreach (var blob in _containerClient.GetBlobsAsync(
            traits: BlobTraits.Metadata,
            states: BlobStates.None,
            prefix: null,
            cancellationToken: CancellationToken.None))
        {
            documents.Add(
                new BlobDocumentInfo
                {
                    FileName = blob.Name,
                    Size = blob.Properties.ContentLength ?? 0,
                    LastModified = blob.Properties.LastModified,
                    ContentType = blob.Properties.ContentType
                        ?? "application/octet-stream"
                });
        }

        return documents
            .OrderBy(x => x.FileName)
            .ToList();
    }

    public async Task<BlobDownloadInfo?> DownloadAsync(string fileName)
    {
        await EnsureContainerExistsAsync();

        var blobClient = _containerClient.GetBlobClient(fileName);

        if (!await blobClient.ExistsAsync())
        {
            return null;
        }

        var response = await blobClient.DownloadAsync();

        return response.Value;
    }

    public async Task<bool> ExistsAsync(string fileName)
    {
        await EnsureContainerExistsAsync();

        return await _containerClient
            .GetBlobClient(fileName)
            .ExistsAsync();
    }
}

public class BlobDocumentInfo
{
    public string FileName { get; set; } = string.Empty;

    public long Size { get; set; }

    public DateTimeOffset? LastModified { get; set; }

    public string ContentType { get; set; } = "application/octet-stream";
}
