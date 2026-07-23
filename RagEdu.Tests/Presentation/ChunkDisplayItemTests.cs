using Assignmet1_Presentation.Models;

namespace RagEdu.Tests.Presentation;

public class ChunkDisplayItemTests
{
    [Fact]
    public void Build_ForPdfChunks_ReturnsOneDisplayItemPerChunkEvenOnSamePage()
    {
        var chunks = new[]
        {
            new ChunkViewModel { ChunkIndex = 0, PageNumber = 3, Content = "first chunk", TokenCount = 2 },
            new ChunkViewModel { ChunkIndex = 1, PageNumber = 3, Content = "second chunk", TokenCount = 2 },
            new ChunkViewModel { ChunkIndex = 2, PageNumber = 4, Content = "third chunk", TokenCount = 2 }
        };

        var items = ChunkDisplayItem.Build(chunks, isSlideDeck: false);

        Assert.Equal(3, items.Count);
        Assert.Collection(
            items,
            item => Assert.Equal("first chunk", item.Content),
            item => Assert.Equal("second chunk", item.Content),
            item => Assert.Equal("third chunk", item.Content));
        Assert.All(items, item => Assert.Equal(1, item.ChunkCount));
    }
}
