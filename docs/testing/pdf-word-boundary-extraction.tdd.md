# PDF word-boundary extraction: TDD evidence

Source plan: user journey derived during this TDD run.

User journey: As a lecturer uploading a PDF, I receive chunks made from separate words rather than a whole page treated as one token.

| # | What is guaranteed | Test | Result |
|---|---|---|---|
| 1 | Page 3 of the supplied Gomaa PDF contains more than 100 whitespace-separated words and preserves the title text | `TextExtractorTests.ExtractPages_GomaaPdf_ReconstructsWordsSeparatedByWhitespace` | PASS |

RED: `dotnet test RagEdu.Tests/RagEdu.Tests.csproj --no-restore --filter FullyQualifiedName~TextExtractorTests --disable-build-servers -m:1 -p:UseSharedCompilation=false` failed because the old `page.Text` output collapsed the page into one whitespace-separated token.

GREEN: the same command passed after PDF parsing changed to `page.GetWords()`.

Coverage: `dotnet test ... --collect:"XPlat Code Coverage"` passed the test and generated `coverage.cobertura.xml`, but the collector did not terminate before the 60-second environment timeout. The report records 27.5% line coverage for `TextExtractor`; DOCX/PPTX branches are not yet covered by this regression test.
