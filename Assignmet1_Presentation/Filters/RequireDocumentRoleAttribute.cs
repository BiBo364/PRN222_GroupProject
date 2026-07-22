using Assignmet1_Presentation.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Filters;

/// <summary>
/// Chỉ cho phép giảng viên đã được phân công môn học tải tài liệu lên.
/// </summary>
public sealed class RequireDocumentUploadAttribute : Attribute, IPageFilter
{
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var roleId = session.GetInt32("RoleId");

        if (roleId is null || !DocumentPermissions.CanUpload(roleId.Value))
        {
            SetError(context, "Chỉ giảng viên mới có quyền tải tài liệu lên.");
            context.Result = new RedirectToPageResult("/Documents/Index");
            return;
        }

        if (DocumentPermissions.GetAssignedSubjectIds(session).Count == 0)
        {
            SetError(
                context,
                "Bạn chưa được phân công môn học. Vui lòng liên hệ quản trị viên.");
            context.Result = new RedirectToPageResult("/Documents/Index");
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }

    private static void SetError(PageHandlerExecutingContext context, string message)
    {
        if (context.HandlerInstance is PageModel pageModel)
        {
            pageModel.TempData["Error"] = message;
        }
    }
}

/// <summary>
/// Chỉ cho phép giảng viên xóa tài liệu.
/// Việc kiểm tra quyền sở hữu theo môn học được thực hiện trong page handler.
/// </summary>
public sealed class RequireDocumentDeleteAttribute : Attribute, IPageFilter
{
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var roleId = context.HttpContext.Session.GetInt32("RoleId");

        if (roleId is null || !DocumentPermissions.CanDelete(roleId.Value))
        {
            if (context.HandlerInstance is PageModel pageModel)
            {
                pageModel.TempData["Error"] = "Bạn không có quyền xóa tài liệu.";
            }

            context.Result = new RedirectToPageResult("/Documents/Index");
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }
}
