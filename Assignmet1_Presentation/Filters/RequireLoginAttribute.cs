using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Assignmet1_Presentation.Filters;

public class RequireLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Session.GetInt32("UserId") is null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        base.OnActionExecuting(context);
    }
}
