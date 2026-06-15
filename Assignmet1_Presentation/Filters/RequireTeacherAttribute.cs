using Assignmet1_Presentation.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Assignmet1_Presentation.Filters;

public class RequireTeacherAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Session.GetInt32("UserId") is null)
        {
            context.Result = new RedirectToPageResult("/Account/Login");
            return;
        }

        var roleId = context.HttpContext.Session.GetInt32("RoleId");
        if (roleId is null || !DocumentPermissions.CanManageSubjects(roleId.Value))
        {
            context.Result = new RedirectToPageResult("/Home/Index");
            return;
        }

        base.OnActionExecuting(context);
    }
}
