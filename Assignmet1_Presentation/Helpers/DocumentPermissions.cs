namespace Assignmet1_Presentation.Helpers;

public static class DocumentPermissions
{
    public const string AssignedSubjectIdsSessionKey = "AssignedSubjectIds";
    public const int AdminRoleId = 1;
    public const int LecturerRoleId = 2;
    public const int StudentRoleId = 3;

    public static bool CanUpload(int roleId) => roleId is LecturerRoleId;

    public static bool CanDelete(int roleId) => roleId is LecturerRoleId;

    public static bool CanManageSubjects(int roleId) => roleId is AdminRoleId;

    public static bool CanView(int roleId) => roleId is AdminRoleId or LecturerRoleId or StudentRoleId;

    public static bool CanUploadToSubject(int roleId, int? userSubjectId, int targetSubjectId)
    {
        if (!CanUpload(roleId))
            return false;

        return userSubjectId.HasValue && userSubjectId.Value == targetSubjectId;
    }

    public static bool CanUploadToAssignedSubject(
        int roleId,
        IEnumerable<int> assignedSubjectIds,
        int targetSubjectId)
        => CanUpload(roleId) && assignedSubjectIds.Contains(targetSubjectId);

    public static bool CanDeleteDocumentFromSubject(
        int roleId,
        ISession session,
        int targetSubjectId)
        => CanDelete(roleId)
            && GetAssignedSubjectIds(session).Contains(targetSubjectId);

    public static IReadOnlyCollection<int> GetAssignedSubjectIds(ISession session)
    {
        var assignedSubjectIds = (session.GetString(AssignedSubjectIdsSessionKey) ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var subjectId) ? subjectId : (int?)null)
            .Where(subjectId => subjectId.HasValue)
            .Select(subjectId => subjectId!.Value)
            .ToHashSet();

        var legacySubjectId = session.GetInt32("SubjectId");
        if (legacySubjectId.HasValue)
            assignedSubjectIds.Add(legacySubjectId.Value);

        return assignedSubjectIds;
    }
}
