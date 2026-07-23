# Multi-subject document deletion: TDD evidence

Source plan: user journey derived during this TDD run.

User journey: As a lecturer assigned to a subject, I can delete that subject's uploaded document even when it is not the legacy single `SubjectId` value.

| # | What is guaranteed | Test | Result |
|---|---|---|
| 1 | A lecturer can delete a document for any subject listed in the authenticated session assignments | `DocumentPermissionsTests.CanDeleteDocumentFromSubject_AllowsAnySubjectAssignedInSession` | PASS |
| 2 | Existing document role filters continue to reject non-lecturers | `PageAuthorizationFilterTests` | PASS |

RED: the new test did not compile because `CanDeleteDocumentFromSubject` was absent.

GREEN: `dotnet test RagEdu.Tests/RagEdu.Tests.csproj --no-restore --filter "FullyQualifiedName~DocumentPermissionsTests|FullyQualifiedName~PageAuthorizationFilterTests" --disable-build-servers -m:1 -p:UseSharedCompilation=false` passed 46 tests.

Coverage: not run for this change; the prior XPlat collector did not terminate within the environment timeout.
