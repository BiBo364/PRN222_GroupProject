using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Helpers;
using Assignmet1_Presentation.Hubs;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace Assignmet1_Presentation.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/documents/{id:int}", async (
            int id,
            HttpContext httpContext,
            IDocumentService documentService,
            IWebHostEnvironment environment,
            IHubContext<AppHub> appHub) =>
        {
            var roleId = httpContext.Session.GetInt32("RoleId");
            if (roleId is null || !DocumentPermissions.CanDelete(roleId.Value))
                return Results.Json(new { message = "Bạn không có quyền xóa tài liệu." }, statusCode: StatusCodes.Status403Forbidden);

            var document = await documentService.GetDocumentByIdAsync(id);
            if (document is null)
                return Results.NotFound(new { message = "Không tìm thấy tài liệu." });

            if (!document.SubjectId.HasValue
                || !DocumentPermissions.CanUploadToSubject(
                    roleId.Value,
                    httpContext.Session.GetInt32("SubjectId"),
                    document.SubjectId.Value))
            {
                return Results.Json(new { message = "Bạn chỉ có thể xóa tài liệu thuộc môn học được phân công." }, statusCode: StatusCodes.Status403Forbidden);
            }

            var deletedDocumentName = document.OriginalName;
            var deletedSubjectId = document.SubjectId;
            var storageRoot = Path.Combine(environment.ContentRootPath, "uploads");
            var deleted = await documentService.DeleteDocumentAsync(
                id,
                storageRoot,
                environment.ContentRootPath,
                environment.WebRootPath,
                httpContext.Session.GetInt32("UserId"));

            if (!deleted)
                return Results.NotFound(new { message = "Không tìm thấy tài liệu." });

            await appHub.Clients.All.SendAsync("DocumentDeleted", id);
            if (deletedSubjectId is int subjectId)
            {
                var subjectService = httpContext.RequestServices.GetRequiredService<ISubjectService>();
                var subject = await subjectService.GetSubjectAsync(subjectId);
                if (subject is not null)
                    await appHub.Clients.All.SendAsync("CourseUpdated", ViewModelMapper.ToListItemViewModel(subject));
            }

            return Results.Ok(new { message = $"Đã chuyển tài liệu vào thùng rác: {deletedDocumentName}." });
        });

        app.MapPost("/api/subjects", async (
            HttpContext httpContext,
            [FromBody] SubjectCreateViewModel model,
            ISubjectService subjectService,
            IHubContext<AppHub> appHub) =>
        {
            var roleId = httpContext.Session.GetInt32("RoleId");
            if (roleId is null || !DocumentPermissions.CanManageSubjects(roleId.Value))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                return Results.BadRequest(new { message = "Mã và tên môn học là bắt buộc." });

            try
            {
                var created = await subjectService.CreateSubjectAsync(model.Code!, model.Name!, model.Description);
                await appHub.Clients.All.SendAsync("CourseCreated", ViewModelMapper.ToListItemViewModel(created));
                return Results.Created($"/Subjects/Details/{created.Subject.Id}", ViewModelMapper.ToViewModel(created));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        return app;
    }
}
