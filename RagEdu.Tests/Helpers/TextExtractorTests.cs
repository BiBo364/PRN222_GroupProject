using Assignment1_Service.Helpers;

namespace RagEdu.Tests.Helpers;

public class TextExtractorTests
{
    [Fact]
    public void ExtractPages_GomaaPdf_ReconstructsWordsSeparatedByWhitespace()
    {
        const string filePath = "C:\\Users\\Surface\\Downloads\\gomaa-softwaremodellinganddesign.pdf";

        var page = TextExtractor.ExtractPages(filePath, "pdf")
            .Single(item => item.PageNumber == 3);

        var words = page.Content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        Assert.True(words.Length > 100);
        Assert.Contains("Software Modeling and Design", page.Content, StringComparison.OrdinalIgnoreCase);
    }
}
