using Assignmet1_Presentation.Helpers;
using RagEdu.Tests.Infrastructure;

namespace RagEdu.Tests.Presentation;

public class DocumentPermissionsTests
{
    [Theory]
    [InlineData(1007)]
    [InlineData(222)]
    [InlineData(392)]
    public void CanUploadToAssignedSubject_AllowsEverySubjectAssignedToLecturer(int subjectId)
    {
        var assignedSubjectIds = new[] { 1007, 222, 392 };

        var allowed = DocumentPermissions.CanUploadToAssignedSubject(
            DocumentPermissions.LecturerRoleId,
            assignedSubjectIds,
            subjectId);

        Assert.True(allowed);
    }

    [Fact]
    public void CanUploadToAssignedSubject_RejectsSubjectNotAssignedToLecturer()
    {
        var allowed = DocumentPermissions.CanUploadToAssignedSubject(
            DocumentPermissions.LecturerRoleId,
            new[] { 1007, 222, 392 },
            999);

        Assert.False(allowed);
    }

    [Fact]
    public void CanDeleteDocumentFromSubject_AllowsAnySubjectAssignedInSession()
    {
        var session = new TestSession();
        session.SetString(DocumentPermissions.AssignedSubjectIdsSessionKey, "1007,1010,392");

        var allowed = DocumentPermissions.CanDeleteDocumentFromSubject(
            DocumentPermissions.LecturerRoleId,
            session,
            1010);

        Assert.True(allowed);
    }
}
