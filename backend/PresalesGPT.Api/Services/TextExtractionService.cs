using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace PresalesGPT.Api.Services;

public class ExtractionResult
{
    public string Text { get; set; } = "";

    public int PageCount { get; set; }
}

public class TextExtractionService
{
    public ExtractionResult Extract(
        string filePath,
        string fileName)
    {
        var extension = Path
            .GetExtension(fileName)
            .ToLowerInvariant();

        if (extension == ".pdf")
        {
            return ExtractFromPdf(filePath);
        }

        if (extension == ".docx")
        {
            return ExtractFromDocx(filePath);
        }

        if (extension == ".txt")
        {
            return new ExtractionResult
            {
                Text = File.ReadAllText(filePath),
                PageCount = 1
            };
        }

        throw new NotSupportedException(
            $"File type '{extension}' is not supported. " +
            "Allowed types are .pdf, .docx and .txt.");
    }

    private ExtractionResult ExtractFromPdf(
        string filePath)
    {
        using var document = PdfDocument.Open(filePath);

        var builder = new System.Text.StringBuilder();

        var pageNumber = 0;

        foreach (var page in document.GetPages())
        {
            pageNumber++;

            builder.AppendLine(
                $"--- Page {pageNumber} ---");

            builder.AppendLine(page.Text);

            builder.AppendLine();
        }

        return new ExtractionResult
        {
            Text = builder.ToString(),
            PageCount = pageNumber
        };
    }

    private ExtractionResult ExtractFromDocx(
        string filePath)
    {
        using var document =
            WordprocessingDocument.Open(filePath, false);

        var body = document.MainDocumentPart?.Document?.Body;

        var text = body?.InnerText ?? "";

        return new ExtractionResult
        {
            Text = text,
            PageCount = 1
        };
    }
}
