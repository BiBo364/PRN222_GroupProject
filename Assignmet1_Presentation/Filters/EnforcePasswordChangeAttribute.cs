using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Assignmet1_Presentation.Filters;

public sealed class EnforcePasswordChangeAttribute : Attribute, IPageFilter
{
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var httpContext = context.HttpContext;
        if (httpContext.Session.GetInt32("UserId") is null)
        {
            return;
        }

        var forcePasswordChange = string.Equals(
            httpContext.Session.GetString("ForcePasswordChange"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!forcePasswordChange)
        {
            return;
        }

        var page = context.RouteData.Values["page"]?.ToString();

        var isAllowedRoute =
            string.Equals(page, "/Account/ChangePassword", StringComparison.OrdinalIgnoreCase)
            || string.Equals(page, "/Account/Logout", StringComparison.OrdinalIgnoreCase);

        if (!isAllowedRoute)
        {
            context.Result = new RedirectToPageResult("/Account/ChangePassword");
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }
}
