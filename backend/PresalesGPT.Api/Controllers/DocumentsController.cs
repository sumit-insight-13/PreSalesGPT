using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresalesGPT.Api.Data;
using PresalesGPT.Api.Models;
using PresalesGPT.Api.Services;

namespace PresalesGPT.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    private static readonly string[] AllowedExtensions =
    {
        ".pdf",
        ".docx",
        ".txt"
    };

    private readonly AppDbContext _db;
    private readonly TextExtractionService _extractor;
    private readonly IWebHostEnvironment _environment;

    public DocumentsController(
        AppDbContext db,
        TextExtractionService extractor,
        IWebHostEnvironment environment)
    {
        _db = db;
        _extractor = extractor;
        _environment = environment;
    }

    [HttpGet("by-opportunity/{opportunityId:int}")]
    public async Task<IActionResult> GetByOpportunity(
        int opportunityId)
    {
        var documents = await _db.Documents
            .Where(d => d.OpportunityId == opportunityId)
            .OrderByDescending(d => d.UploadedOn)
            .Select(d => new
            {
                d.Id,
                d.OpportunityId,
                d.FileName,
                d.FileSizeBytes,
                d.PageCount,
                d.Status,
                d.ErrorMessage,
                d.UploadedOn,
                TextLength = d.ExtractedText.Length
            })
            .ToListAsync();

        return Ok(documents);
    }

    [HttpGet("{id:int}/text")]
    public async Task<IActionResult> GetText(int id)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document is null)
        {
            return NotFound(new
            {
                message = $"Document {id} not found."
            });
        }

        return Ok(new
        {
            document.Id,
            document.FileName,
            document.PageCount,
            document.Status,
            document.ExtractedText
        });
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> Upload(
        [FromForm] int opportunityId,
        IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "File is required."
            });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return BadRequest(new
            {
                message = "File size cannot exceed 20 MB."
            });
        }

        var extension = Path
            .GetExtension(file.FileName)
            .ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                message =
                    "Only .pdf, .docx and .txt files are allowed."
            });
        }

        var opportunityExists = await _db.Opportunities
            .AnyAsync(o => o.Id == opportunityId);

        if (!opportunityExists)
        {
            return BadRequest(new
            {
                message =
                    $"Opportunity {opportunityId} does not exist."
            });
        }

        var uploadsRoot = Path.Combine(
            _environment.ContentRootPath,
            "Uploads");

        Directory.CreateDirectory(uploadsRoot);

        var safeName =
            $"{Guid.NewGuid():N}{extension}";

        var storedPath = Path.Combine(
            uploadsRoot,
            safeName);

        using (var stream = System.IO.File.Create(storedPath))
        {
            await file.CopyToAsync(stream);
        }

        var document = new Document
        {
            OpportunityId = opportunityId,
            FileName = file.FileName,
            ContentType = file.ContentType ?? "",
            FileSizeBytes = file.Length,
            StoredPath = storedPath,
            Status = "Processing",
            UploadedOn = DateTime.UtcNow
        };

        _db.Documents.Add(document);

        await _db.SaveChangesAsync();

        try
        {
            var result = _extractor.Extract(
                storedPath,
                file.FileName);

            document.ExtractedText = result.Text;
            document.PageCount = result.PageCount;
            document.Status = "Extracted";
            document.ErrorMessage = null;
        }
        catch (Exception exception)
        {
            document.Status = "Failed";
            document.ErrorMessage = exception.Message;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            document.Id,
            document.OpportunityId,
            document.FileName,
            document.FileSizeBytes,
            document.PageCount,
            document.Status,
            document.ErrorMessage,
            document.UploadedOn,
            TextPreview = document.ExtractedText.Length > 500
                ? document.ExtractedText[..500]
                : document.ExtractedText
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document is null)
        {
            return NotFound(new
            {
                message = $"Document {id} not found."
            });
        }

        if (System.IO.File.Exists(document.StoredPath))
        {
            System.IO.File.Delete(document.StoredPath);
        }

        _db.Documents.Remove(document);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Document deleted."
        });
    }
}
