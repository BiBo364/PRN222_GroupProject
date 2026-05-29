namespace Assignmet1_Presentation.Helpers;

public static class SubscriptionPermissions
{
    public static bool IsAdmin(int roleId) => roleId is 1 or 2;

    public static bool CanBypassSubscription(int roleId) => IsAdmin(roleId);
}
