using CoffeeNChill.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public class DocumentFunctions
{
    private readonly BlobStorageService _blobStorageService;
    private readonly ILogger<DocumentFunctions> _logger;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".doc",
            ".docx",
            ".txt",
            ".jpg",
            ".jpeg",
            ".png"
        };

    public DocumentFunctions(
        BlobStorageService blobStorageService,
        ILogger<DocumentFunctions> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    [Function("UploadStaffDocument")]
    public async Task<IActionResult> UploadStaffDocument(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "documents/upload")]
        HttpRequest req)
    {
        try
        {
            if (!req.HasFormContentType)
            {
                return new BadRequestObjectResult(new
                {
                    error = "Content-Type must be multipart/form-data."
                });
            }

            var form = await req.ReadFormAsync();

            if (form.Files.Count == 0)
            {
                return new BadRequestObjectResult(new
                {
                    error = "No file was supplied. Use the form field name 'file'."
                });
            }

            var file = form.Files["file"] ?? form.Files[0];

            if (file.Length == 0)
            {
                return new BadRequestObjectResult(new
                {
                    error = "The uploaded file is empty."
                });
            }

            var originalName = Path.GetFileName(file.FileName);

            if (string.IsNullOrWhiteSpace(originalName))
            {
                return new BadRequestObjectResult(new
                {
                    error = "A valid file name is required."
                });
            }

            var extension = Path.GetExtension(originalName);

            if (!AllowedExtensions.Contains(extension))
            {
                return new BadRequestObjectResult(new
                {
                    error = "Unsupported file type.",
                    allowedTypes = AllowedExtensions.OrderBy(x => x)
                });
            }

            var safeFileName =
                $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}_{originalName}";

            await using var stream = file.OpenReadStream();

            await _blobStorageService.UploadAsync(
                safeFileName,
                stream,
                file.ContentType);

            _logger.LogInformation(
                "Staff document uploaded: {FileName}, {Size} bytes.",
                safeFileName,
                file.Length);

            return new OkObjectResult(new
            {
                message = "Document uploaded successfully.",
                fileName = safeFileName,
                originalFileName = originalName,
                size = file.Length,
                contentType = file.ContentType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload staff document.");

            return new ObjectResult(new
            {
                error = "An unexpected error occurred while uploading the document."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("ListStaffDocuments")]
    public async Task<IActionResult> ListStaffDocuments(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "documents")]
        HttpRequest req)
    {
        try
        {
            var documents = await _blobStorageService.ListAsync();

            return new OkObjectResult(new
            {
                container = "staff-docs",
                count = documents.Count,
                documents
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list staff documents.");

            return new ObjectResult(new
            {
                error = "An unexpected error occurred while listing documents."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("DownloadStaffDocument")]
    public async Task<IActionResult> DownloadStaffDocument(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "documents/download/{fileName}")]
        HttpRequest req,
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new BadRequestObjectResult(new
            {
                error = "File name is required."
            });
        }

        var safeFileName = Path.GetFileName(fileName);

        if (!string.Equals(fileName, safeFileName, StringComparison.Ordinal))
        {
            return new BadRequestObjectResult(new
            {
                error = "Invalid file name."
            });
        }

        try
        {
            var download = await _blobStorageService.DownloadAsync(safeFileName);

            if (download is null)
            {
                return new NotFoundObjectResult(new
                {
                    error = "Document not found."
                });
            }

            var contentType =
                download.Details.ContentType
                ?? "application/octet-stream";

            return new FileStreamResult(
                download.Content,
                contentType)
            {
                FileDownloadName = safeFileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to download staff document {FileName}.",
                safeFileName);

            return new ObjectResult(new
            {
                error = "An unexpected error occurred while downloading the document."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
