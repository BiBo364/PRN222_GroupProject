namespace Assignmet1_Presentation.Helpers;

public static class SubscriptionPermissions
{
    public static bool IsAdmin(int roleId) => roleId is DocumentPermissions.SuperAdminRoleId;

    public static bool CanBypassSubscription(int roleId) => IsAdmin(roleId);
}
