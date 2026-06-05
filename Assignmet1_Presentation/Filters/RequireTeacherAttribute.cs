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
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var roleId = context.HttpContext.Session.GetInt32("RoleId");
        // RoleId 1 (SuperAdmin), 2 (Admin), 3 (Teacher)
        if (roleId is null || (roleId.Value != 1 && roleId.Value != 2 && roleId.Value != 3))
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
            return;
        }

        base.OnActionExecuting(context);
    }
}
