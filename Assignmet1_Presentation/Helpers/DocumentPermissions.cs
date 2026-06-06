namespace Assignmet1_Presentation.Helpers;

public static class DocumentPermissions
{
    public const int SuperAdminRoleId = 1;
    public const int TeacherRoleId = 2;
    public const int StudentRoleId = 4;

    public static bool CanUpload(int roleId) => roleId is SuperAdminRoleId or TeacherRoleId;

    public static bool CanDelete(int roleId) => roleId is SuperAdminRoleId;

    public static bool CanManageSubjects(int roleId) => roleId is SuperAdminRoleId;

    public static bool CanView(int roleId) => roleId is SuperAdminRoleId or TeacherRoleId or 3 or StudentRoleId;

    public static bool CanUploadToSubject(int roleId, int? userSubjectId, int targetSubjectId)
    {
        if (!CanUpload(roleId))
            return false;

        if (roleId == SuperAdminRoleId)
            return true;

        return userSubjectId.HasValue && userSubjectId.Value == targetSubjectId;
    }
}
