using Assignmet1_Presentation.Filters;
using Microsoft.AspNetCore.Mvc;
using RagEdu.Tests.Infrastructure;

namespace RagEdu.Tests.Presentation;

public sealed class PageAuthorizationFilterTests
{
    [Fact]
    public void RequireLogin_RedirectsAnonymousUserToLogin()
    {
        var context = PageFilterTestContext.Create();

        new RequireLoginAttribute().OnPageHandlerExecuting(context);

        var redirect = Assert.IsType<RedirectToPageResult>(context.Result);
        Assert.Equal("/Account/Login", redirect.PageName);
    }

    [Fact]
    public void RequireLogin_AllowsAuthenticatedUser()
    {
        var context = PageFilterTestContext.Create(userId: 10);

        new RequireLoginAttribute().OnPageHandlerExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void RequireAdmin_RedirectsAnonymousUserToLogin()
    {
        var context = PageFilterTestContext.Create();

        new RequireAdminAttribute().OnPageHandlerExecuting(context);

        var redirect = Assert.IsType<RedirectToPageResult>(context.Result);
        Assert.Equal("/Account/Login", redirect.PageName);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void RequireAdmin_RedirectsNonAdministratorToHome(int roleId)
    {
        var context = PageFilterTestContext.Create(userId: 10, roleId: roleId);

        new RequireAdminAttribute().OnPageHandlerExecuting(context);

        var redirect = Assert.IsType<RedirectToPageResult>(context.Result);
        Assert.Equal("/Home/Index", redirect.PageName);
    }

    [Fact]
    public void RequireAdmin_AllowsAdministrator()
    {
        var context = PageFilterTestContext.Create(userId: 10, roleId: 1);

        new RequireAdminAttribute().OnPageHandlerExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void RequireLecturer_RedirectsAnonymousUserToLogin()
    {
        var context = PageFilterTestContext.Create();

        new RequireLecturerAttribute().OnPageHandlerExecuting(context);

        var redirect = Assert.IsType<RedirectToPageResult>(context.Result);
        Assert.Equal("/Account/Login", redirect.PageName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public void RequireLecturer_RedirectsOtherRolesToHome(int roleId)
    {
        var context = PageFilterTestContext.Create(userId: 10, roleId: roleId);

        new RequireLecturerAttribute().OnPageHandlerExecuting(context);

        var redirect = Assert.IsType<RedirectToPageResult>(context.Result);
        Assert.Equal("/Home/Index", redirect.PageName);
    }

    [Fact]
    public void RequireLecturer_AllowsLecturer()
    {
        var context = PageFilterTestContext.Create(userId: 10, roleId: 2);

        new RequireLecturerAttribute().OnPageHandlerExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void RequireAuditRole_RedirectsAnonymousUserToLogin()
    {
        var context = PageFilterTestContext.Create();

        new RequireAuditRoleAttribute().OnPageHandlerExecuting(context);

        var redirect = Assert.IsType<RedirectToPageResult>(context.Result);
        Assert.Equal("/Account/Login", redirect.PageName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void RequireAuditRole_AllowsAdministratorAndLecturer(int roleId)
    {
        var context = PageFilterTestContext.Create(userId: 10, roleId: roleId);

        new RequireAuditRoleAttribute().OnPageHandlerExecuting(context);

        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(99)]
    public void RequireAuditRole_RedirectsOtherRolesToHome(int roleId)
    {
        var context = PageFilterTestContext.Create(userId: 10, roleId: roleId);

        new RequireAuditRoleAttribute().OnPageHandlerExecuting(context);

        var redirect = Assert.IsType<RedirectToPageResult>(context.Result);
        Assert.Equal("/Home/Index", redirect.PageName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public void RequireDocumentUpload_RejectsNonLecturer(int roleId)
    {
        var context = PageFilterTestContext.Create(
            userId: 10,
            roleId: roleId,
            subjectId: 1);

        new RequireDocumentUploadAttribute().OnPageHandlerExecuting(context);

        var redirect = Assert.IsType<RedirectToPageResult>(context.Result);
        Assert.Equal("/Documents/Index", redirect.PageName);
    }

    [Fact]
    public void RequireDocumentUpload_RejectsLecturerWithoutAssignedSubject()
    {
        var context = PageFilterTestContext.Create(userId: 10, roleId: 2);

        new RequireDocumentUploadAttribute().OnPageHandlerExecuting(context);

        var redirect = Assert.IsType<RedirectToPageResult>(context.Result);
        Assert.Equal("/Documents/Index", redirect.PageName);
    }

    [Fact]
    public void RequireDocumentUpload_AllowsAssignedLecturer()
    {
        var context = PageFilterTestContext.Create(
            userId: 10,
            roleId: 2,
            subjectId: 1);

        new RequireDocumentUploadAttribute().OnPageHandlerExecuting(context);

        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public void RequireDocumentDelete_RejectsNonLecturer(int roleId)
    {
        var context = PageFilterTestContext.Create(userId: 10, roleId: roleId);

        new RequireDocumentDeleteAttribute().OnPageHandlerExecuting(context);

        var redirect = Assert.IsType<RedirectToPageResult>(context.Result);
        Assert.Equal("/Documents/Index", redirect.PageName);
    }

    [Fact]
    public void RequireDocumentDelete_AllowsLecturer()
    {
        var context = PageFilterTestContext.Create(userId: 10, roleId: 2);

        new RequireDocumentDeleteAttribute().OnPageHandlerExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void EnforcePasswordChange_IgnoresAnonymousUser()
    {
        var context = PageFilterTestContext.Create(
            forcePasswordChange: "true",
            page: "/Learning/Index");

        new EnforcePasswordChangeAttribute().OnPageHandlerExecuting(context);

        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("false")]
    [InlineData("FALSE")]
    [InlineData("no")]
    public void EnforcePasswordChange_AllowsUserWithoutForceFlag(string? flag)
    {
        var context = PageFilterTestContext.Create(
            userId: 10,
            forcePasswordChange: flag,
            page: "/Learning/Index");

        new EnforcePasswordChangeAttribute().OnPageHandlerExecuting(context);

        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData("/Account/ChangePassword")]
    [InlineData("/account/changepassword")]
    [InlineData("/Account/Logout")]
    [InlineData("/ACCOUNT/LOGOUT")]
    public void EnforcePasswordChange_AllowsPasswordAndLogoutPages(string page)
    {
        var context = PageFilterTestContext.Create(
            userId: 10,
            forcePasswordChange: "true",
            page: page);

        new EnforcePasswordChangeAttribute().OnPageHandlerExecuting(context);

        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData("/Learning/Index")]
    [InlineData("/Home/Index")]
    [InlineData("/Documents/Index")]
    [InlineData("/Admin/Index")]
    public void EnforcePasswordChange_RedirectsOtherPages(string page)
    {
        var context = PageFilterTestContext.Create(
            userId: 10,
            forcePasswordChange: "TRUE",
            page: page);

        new EnforcePasswordChangeAttribute().OnPageHandlerExecuting(context);

        var redirect = Assert.IsType<RedirectToPageResult>(context.Result);
        Assert.Equal("/Account/ChangePassword", redirect.PageName);
    }
}
