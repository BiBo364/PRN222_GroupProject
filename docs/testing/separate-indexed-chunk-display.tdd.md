# Separate indexed chunk display: TDD evidence

Source plan: user journey derived during this TDD run.

User journey: As a lecturer viewing an indexed PDF, I can see each stored chunk separately so its token count is not confused with the total of a page.

| # | What is guaranteed | Test | Result |
|---|---|---|
| 1 | Two chunks on the same PDF page render as two display items | `ChunkDisplayItemTests.Build_ForPdfChunks_ReturnsOneDisplayItemPerChunkEvenOnSamePage` | PASS |
| 2 | Each display item contains exactly one chunk | same test | PASS |

RED: the test expected 3 display items but received 2 because chunks were grouped by page.

GREEN: `dotnet test RagEdu.Tests/RagEdu.Tests.csproj --no-restore --filter FullyQualifiedName~ChunkDisplayItemTests --disable-build-servers -m:1 -p:UseSharedCompilation=false` passed 1 test.

Coverage: not run; the XPlat coverage collector previously exceeded the environment timeout after producing its report.
