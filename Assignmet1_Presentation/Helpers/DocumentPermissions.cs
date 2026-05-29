namespace Assignmet1_Presentation.Helpers;

public static class DocumentPermissions
{
    public static bool CanUpload(int roleId) => roleId is 1 or 2 or 3;

    public static bool CanDelete(int roleId) => roleId is 1 or 2;

    public static bool CanView(int roleId) => roleId is 1 or 2 or 3 or 4;
}
