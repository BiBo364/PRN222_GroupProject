# Multi-subject upload access: TDD evidence

Source plan: user journey derived during this TDD run.

User journey: As a lecturer assigned to PRU, PRN222, and SWD392, I can upload a document to each assigned subject.

| # | What is guaranteed | Test | Result |
|---|---|---|---|
| 1 | Every assigned subject is accepted for upload | `DocumentPermissionsTests.CanUploadToAssignedSubject_AllowsEverySubjectAssignedToLecturer` | PASS |
| 2 | An unassigned subject is rejected | `DocumentPermissionsTests.CanUploadToAssignedSubject_RejectsSubjectNotAssignedToLecturer` | PASS |
| 3 | Existing document-upload page authorization remains correct | `PageAuthorizationFilterTests` | PASS |

RED: `dotnet test RagEdu.Tests/RagEdu.Tests.csproj --no-restore --filter FullyQualifiedName~DocumentPermissionsTests --disable-build-servers -m:1 -p:UseSharedCompilation=false` failed because `CanUploadToAssignedSubject` did not exist.

GREEN: `dotnet test RagEdu.Tests/RagEdu.Tests.csproj --no-restore --filter "FullyQualifiedName~DocumentPermissionsTests|FullyQualifiedName~PageAuthorizationFilterTests" --disable-build-servers -m:1 -p:UseSharedCompilation=false` passed 45 tests.

Coverage: the same filtered test command with `--collect:"XPlat Code Coverage"` exceeded the 60-second environment timeout, so no coverage percentage is claimed.
