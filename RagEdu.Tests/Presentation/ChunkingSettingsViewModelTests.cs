using System.ComponentModel.DataAnnotations;
using Assignmet1_Presentation.Models;

namespace RagEdu.Tests.Presentation;

public class ChunkingSettingsViewModelTests
{
    [Fact]
    public void Validate_FailsWhenChunkOverlapIsNotSmallerThanChunkSize()
    {
        var model = new ChunkingSettingsViewModel
        {
            ChunkSize = 250,
            ChunkOverlap = 250
        };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), validationResults, true);

        Assert.False(isValid);
    }
}
