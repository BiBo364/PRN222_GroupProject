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
        return string.Join("\n", ExtractPages(filePath, fileType).Select(page => page.Content));
    }

    public static List<PageTextSegment> ExtractPages(string filePath, string fileType)
    {
        return fileType switch
        {
            "pdf" => ExtractFromPdf(filePath),
            "docx" => ExtractFromDocx(filePath),
            "pptx" => ExtractFromPptx(filePath),
            _ => throw new NotSupportedException($"Loại tệp '{fileType}' không được hỗ trợ.")
        };
    }

    private static List<PageTextSegment> ExtractFromPdf(string filePath)
    {
        var pages = new List<PageTextSegment>();
        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            var text = page.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                pages.Add(new PageTextSegment { PageNumber = page.Number, Content = text });
        }

        return pages;
    }

    private static List<PageTextSegment> ExtractFromDocx(string filePath)
    {
        var builder = new StringBuilder();
        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null)
            return [];

        foreach (var paragraph in body.Elements<Paragraph>())
        {
            builder.AppendLine(paragraph.InnerText);
        }

        var text = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(text)
            ? []
            : [new PageTextSegment { PageNumber = 1, Content = text }];
    }

    private static List<PageTextSegment> ExtractFromPptx(string filePath)
    {
        var pages = new List<PageTextSegment>();
        using var document = PresentationDocument.Open(filePath, false);
        var slides = document.PresentationPart?.SlideParts;
        if (slides is null)
            return pages;

        foreach (var slide in slides)
        {
            var builder = new StringBuilder();
            foreach (var text in slide.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
            {
                if (!string.IsNullOrWhiteSpace(text.Text))
                    builder.AppendLine(text.Text);
            }

            var slideText = builder.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(slideText))
            {
                pages.Add(new PageTextSegment
                {
                    PageNumber = pages.Count + 1,
                    Content = slideText
                });
            }
        }

        return pages;
    }
}
