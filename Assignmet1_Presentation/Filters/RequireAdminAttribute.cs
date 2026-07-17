using Assignmet1_Presentation.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Assignmet1_Presentation.Filters;

public sealed class RequireAdminAttribute : Attribute, IPageFilter
{
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (context.HttpContext.Session.GetInt32("UserId") is null)
        {
            context.Result = new RedirectToPageResult("/Account/Login");
            return;
        }

        var roleId = context.HttpContext.Session.GetInt32("RoleId");
        if (roleId is null || !SubscriptionPermissions.IsAdmin(roleId.Value))
        {
            context.Result = new RedirectToPageResult("/Home/Index");
            return;
        }

    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }
}
