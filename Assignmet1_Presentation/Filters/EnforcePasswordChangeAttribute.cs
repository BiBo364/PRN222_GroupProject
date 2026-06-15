using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Assignmet1_Presentation.Filters;

public class EnforcePasswordChangeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var httpContext = context.HttpContext;
        if (httpContext.Session.GetInt32("UserId") is null)
        {
            base.OnActionExecuting(context);
            return;
        }

        var forcePasswordChange = string.Equals(
            httpContext.Session.GetString("ForcePasswordChange"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!forcePasswordChange)
        {
            base.OnActionExecuting(context);
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();

        var isAllowedRoute =
            string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(action, "ChangePassword", StringComparison.OrdinalIgnoreCase)
                || string.Equals(action, "Logout", StringComparison.OrdinalIgnoreCase));

        if (!isAllowedRoute)
        {
            context.Result = new RedirectToActionResult("ChangePassword", "Account", null);
            return;
        }

        base.OnActionExecuting(context);
    }
}
