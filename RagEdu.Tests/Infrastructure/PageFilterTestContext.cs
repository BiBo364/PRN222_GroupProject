using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace RagEdu.Tests.Infrastructure;

internal static class PageFilterTestContext
{
    public static PageHandlerExecutingContext Create(
        int? userId = null,
        int? roleId = null,
        int? subjectId = null,
        string page = "/Learning/Index",
        string? forcePasswordChange = null,
        object? handlerInstance = null)
    {
        var httpContext = new DefaultHttpContext();
        var session = new TestSession();
        httpContext.Features.Set<ISessionFeature>(new TestSessionFeature
        {
            Session = session
        });
        if (userId.HasValue)
            session.SetInt32("UserId", userId.Value);
        if (roleId.HasValue)
            session.SetInt32("RoleId", roleId.Value);
        if (subjectId.HasValue)
            session.SetInt32("SubjectId", subjectId.Value);
        if (forcePasswordChange is not null)
            session.SetString("ForcePasswordChange", forcePasswordChange);

        var routeData = new RouteData();
        routeData.Values["page"] = page;
        var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(
            httpContext,
            routeData,
            new ActionDescriptor());
        var pageContext = new PageContext(actionContext);
        return new PageHandlerExecutingContext(
            pageContext,
            [],
            new HandlerMethodDescriptor(),
            new Dictionary<string, object?>(),
            handlerInstance ?? new object());
    }

    private sealed class TestSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }
}
