using Assignment1_Repository.Models;

namespace Assignment1_Service.Helpers;

public static class DocumentPathResolver
{
    public static string? Resolve(Document document, string storageRoot, string contentRoot, string webRoot)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(document.StoragePath))
        {
            candidates.Add(document.StoragePath);

            var normalized = document.StoragePath
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            candidates.Add(Path.Combine(contentRoot, normalized));

            if (document.StoragePath.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(document.StoragePath);
                candidates.Add(Path.Combine(storageRoot, fileName));
                candidates.Add(Path.Combine(webRoot, "uploads", fileName));
            }
        }

        if (!string.IsNullOrWhiteSpace(document.Filename))
        {
            candidates.Add(Path.Combine(storageRoot, document.Filename));
            candidates.Add(Path.Combine(contentRoot, "uploads", document.Filename));
            candidates.Add(Path.Combine(webRoot, "uploads", document.Filename));
        }

        return candidates
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(File.Exists);
    }
}
