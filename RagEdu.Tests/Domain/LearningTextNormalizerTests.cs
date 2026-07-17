namespace RagEdu.Tests.Domain;

public sealed class LearningTextNormalizerTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("ASP.NET Core", "asp net core")]
    [InlineData("Dependency   Injection", "dependency injection")]
    [InlineData("Đúng", "dung")]
    [InlineData("DUNG", "dung")]
    [InlineData("Lập trình hướng đối tượng", "lap trinh huong doi tuong")]
    [InlineData("  Cơ-sở/dữ_liệu  ", "co so du lieu")]
    [InlineData("Razor\tPages\r\nMVC", "razor pages mvc")]
    [InlineData("Quiz 123 phiên bản 2", "quiz 123 phien ban 2")]
    public void NormalizeForComparison_ReturnsCanonicalTokens(
        string? input,
        string expected)
    {
        var result = LearningTextNormalizer.NormalizeForComparison(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Đúng", "Dung")]
    [InlineData("Cơ sở dữ liệu", "CO-SO-DU-LIEU")]
    [InlineData("Dependency Injection", "dependency_injection")]
    [InlineData("ASP.NET Core", "asp/net/core")]
    [InlineData("Lập trình Web", "  LAP   TRINH   WEB  ")]
    public void NormalizeForComparison_ProducesSameValueForEquivalentAnswers(
        string left,
        string right)
    {
        Assert.Equal(
            LearningTextNormalizer.NormalizeForComparison(left),
            LearningTextNormalizer.NormalizeForComparison(right));
    }

    [Theory]
    [InlineData("Đúng", "Sai")]
    [InlineData("ASP.NET Core", "ASP.NET Framework")]
    [InlineData("Quiz", "Quiz khác")]
    [InlineData("Giảng viên", "Sinh viên")]
    public void NormalizeForComparison_PreservesSemanticDifferences(
        string left,
        string right)
    {
        Assert.NotEqual(
            LearningTextNormalizer.NormalizeForComparison(left),
            LearningTextNormalizer.NormalizeForComparison(right));
    }
}
