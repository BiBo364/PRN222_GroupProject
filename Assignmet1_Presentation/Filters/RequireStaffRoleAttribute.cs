using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Assignmet1_Presentation.Filters;

public sealed class RequireLecturerAttribute : Attribute, IPageFilter
{
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (session.GetInt32("UserId") is null)
        {
            context.Result = new RedirectToPageResult("/Account/Login");
            return;
        }

        if (session.GetInt32("RoleId") != 2)
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

public sealed class RequireAuditRoleAttribute : Attribute, IPageFilter
{
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (session.GetInt32("UserId") is null)
        {
            context.Result = new RedirectToPageResult("/Account/Login");
            return;
        }

        if (session.GetInt32("RoleId") is not 1 and not 2)
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
