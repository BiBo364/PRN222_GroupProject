using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace Assignment1_Service.Helpers;

public static class TextExtractor
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".pptx"
    };

    public static bool IsAllowedExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return AllowedExtensions.Contains(ext);
    }

    public static string GetFileType(string fileName)
    {
        return Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
    }

    public static string ExtractText(string filePath, string fileType)
    {
        return fileType switch
        {
            "pdf" => ExtractFromPdf(filePath),
            "docx" => ExtractFromDocx(filePath),
            "pptx" => ExtractFromPptx(filePath),
            _ => throw new NotSupportedException($"File type '{fileType}' is not supported.")
        };
    }

    private static string ExtractFromPdf(string filePath)
    {
        var builder = new StringBuilder();
        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }
        return builder.ToString();
    }

    private static string ExtractFromDocx(string filePath)
    {
        var builder = new StringBuilder();
        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null)
            return string.Empty;

        foreach (var paragraph in body.Elements<Paragraph>())
        {
            builder.AppendLine(paragraph.InnerText);
        }
        return builder.ToString();
    }

    private static string ExtractFromPptx(string filePath)
    {
        var builder = new StringBuilder();
        using var document = PresentationDocument.Open(filePath, false);
        var slides = document.PresentationPart?.SlideParts;
        if (slides is null)
            return string.Empty;

        foreach (var slide in slides)
        {
            foreach (var text in slide.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
            {
                if (!string.IsNullOrWhiteSpace(text.Text))
                    builder.AppendLine(text.Text);
            }
        }
        return builder.ToString();
    }
}
