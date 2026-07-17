using System.Globalization;
using System.Text;

namespace Assignment1_Repository.Models;

public static class LearningTextNormalizer
{
    public static string NormalizeForComparison(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(character switch
            {
                'Đ' or 'đ' => 'd',
                _ when char.IsLetterOrDigit(character) => char.ToLowerInvariant(character),
                _ => ' '
            });
        }

        return string.Join(
            ' ',
            builder.ToString().Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries));
    }
}
