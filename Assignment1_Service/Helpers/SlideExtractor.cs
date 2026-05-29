using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DrawingText = DocumentFormat.OpenXml.Drawing.Text;

namespace Assignment1_Service.Helpers;

public static class SlideExtractor
{
    public static List<SlideContent> ExtractSlides(string filePath, int documentId, string webRoot)
    {
        var slides = new List<SlideContent>();
        var outputDir = System.IO.Path.Combine(webRoot, "slide-images", documentId.ToString());
        Directory.CreateDirectory(outputDir);

        using var presentation = PresentationDocument.Open(filePath, false);
        var presentationPart = presentation.PresentationPart;
        if (presentationPart?.Presentation?.SlideIdList is null)
            return slides;

        var slideNumber = 0;
        foreach (var slideId in presentationPart.Presentation.SlideIdList.Elements<SlideId>())
        {
            slideNumber++;
            var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
            var text = ExtractSlideText(slidePart);
            var imageUrls = ExtractSlideImages(slidePart, outputDir, documentId, slideNumber);

            if (string.IsNullOrWhiteSpace(text) && imageUrls.Count == 0)
                continue;

            slides.Add(new SlideContent
            {
                SlideNumber = slideNumber,
                Text = string.IsNullOrWhiteSpace(text) ? $"[Slide {slideNumber}]" : text.Trim(),
                ImageUrls = imageUrls
            });
        }

        return slides;
    }

    public static void DeleteSlideImages(int documentId, string webRoot)
    {
        var dir = System.IO.Path.Combine(webRoot, "slide-images", documentId.ToString());
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
    }

    private static string ExtractSlideText(SlidePart slidePart)
    {
        var builder = new StringBuilder();
        foreach (var text in slidePart.Slide.Descendants<DrawingText>())
        {
            if (!string.IsNullOrWhiteSpace(text.Text))
                builder.AppendLine(text.Text);
        }
        return builder.ToString().Trim();
    }

    private static List<string> ExtractSlideImages(
        SlidePart slidePart,
        string outputDir,
        int documentId,
        int slideNumber)
    {
        var urls = new List<string>();
        var imageIndex = 0;

        foreach (var imagePart in slidePart.ImageParts
                     .GroupBy(p => p.Uri)
                     .Select(g => g.First()))
        {
            var extension = GetImageExtension(imagePart.ContentType);
            var fileName = $"slide_{slideNumber}_{imageIndex}{extension}";
            var filePath = System.IO.Path.Combine(outputDir, fileName);

            using (var stream = File.Create(filePath))
            using (var source = imagePart.GetStream())
            {
                source.CopyTo(stream);
            }

            urls.Add($"/slide-images/{documentId}/{fileName}");
            imageIndex++;
        }

        return urls;
    }

    private static string GetImageExtension(string? contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/jpeg" or "image/jpg" => ".jpg",
        "image/gif" => ".gif",
        "image/bmp" => ".bmp",
        "image/tiff" => ".tiff",
        _ => ".png"
    };
}
