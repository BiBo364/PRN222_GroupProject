using System.ComponentModel.DataAnnotations;

namespace Assignmet1_Presentation.Models;

public class ChunkingSettingsViewModel : IValidatableObject
{
    [Display(Name = "Chunk size (số từ)")]
    [Range(1, 2000, ErrorMessage = "Chunk size phải nằm trong khoảng từ 1 đến 2000 từ.")]
    public int ChunkSize { get; set; }

    [Display(Name = "Chunk overlap (số từ)")]
    [Range(0, 1999, ErrorMessage = "Chunk overlap phải nằm trong khoảng từ 0 đến 1999 từ.")]
    public int ChunkOverlap { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChunkOverlap >= ChunkSize)
        {
            yield return new ValidationResult(
                "Chunk overlap phải nhỏ hơn chunk size.",
                [nameof(ChunkOverlap)]);
        }
    }
}
