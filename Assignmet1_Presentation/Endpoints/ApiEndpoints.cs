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
                return Results.Forbid();

            var document = await documentService.GetDocumentByIdAsync(id);
            var storageRoot = Path.Combine(environment.ContentRootPath, "uploads");
            var deleted = await documentService.DeleteDocumentAsync(
                id, storageRoot, environment.ContentRootPath, environment.WebRootPath);

            if (!deleted)
                return Results.NotFound(new { message = "Document not found." });

            await appHub.Clients.All.SendAsync("DocumentDeleted", id);
            if (document?.SubjectId is int subjectId)
            {
                var subjectService = httpContext.RequestServices.GetRequiredService<ISubjectService>();
                var subject = await subjectService.GetSubjectAsync(subjectId);
                if (subject is not null)
                    await appHub.Clients.All.SendAsync("CourseUpdated", ViewModelMapper.ToListItemViewModel(subject));
            }

            return Results.NoContent();
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
                return Results.BadRequest(new { message = "Code and Name are required." });

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
