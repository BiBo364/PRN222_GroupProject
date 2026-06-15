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

        var page = context.RouteData.Values["page"]?.ToString();

        var isAllowedRoute =
            string.Equals(page, "/Account/ChangePassword", StringComparison.OrdinalIgnoreCase)
            || string.Equals(page, "/Account/Logout", StringComparison.OrdinalIgnoreCase);

        if (!isAllowedRoute)
        {
            context.Result = new RedirectToPageResult("/Account/ChangePassword");
            return;
        }

        base.OnActionExecuting(context);
    }
}
