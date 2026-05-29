using Assignmet1_Presentation.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Assignmet1_Presentation.Filters;

public class RequireDocumentUploadAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var roleId = context.HttpContext.Session.GetInt32("RoleId");
        if (roleId is null || !DocumentPermissions.CanUpload(roleId.Value))
        {
            if (context.Controller is Controller controller)
                controller.TempData["Error"] = "You do not have permission to upload documents.";

            context.Result = new RedirectToActionResult("Index", "Documents", null);
        }
    }
}

public class RequireDocumentDeleteAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var roleId = context.HttpContext.Session.GetInt32("RoleId");
        if (roleId is null || !DocumentPermissions.CanDelete(roleId.Value))
        {
            if (context.Controller is Controller controller)
                controller.TempData["Error"] = "You do not have permission to delete documents.";

            context.Result = new RedirectToActionResult("Index", "Documents", null);
        }
    }
}
