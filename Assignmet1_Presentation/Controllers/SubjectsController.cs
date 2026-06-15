using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Helpers;
using Assignmet1_Presentation.Hubs;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Assignmet1_Presentation.Controllers;

[RequireLogin]
public class SubjectsController : Controller
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<AppHub> _appHub;

    public SubjectsController(
        ISubjectService subjectService,
        IHubContext<AppHub> appHub)
    {
        _subjectService = subjectService;
        _appHub = appHub;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Index", "Documents");
    }

    [RequireTeacher]
    public async Task<IActionResult> Manage()
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        return View(subjects.Select(ViewModelMapper.ToViewModel).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!CanViewSubjects())
            return RedirectToAction("Login", "Account");

        var subject = await _subjectService.GetSubjectAsync(id);
        if (subject is null)
            return NotFound();

        var roleId = HttpContext.Session.GetInt32("RoleId")!.Value;
        var userSubjectId = HttpContext.Session.GetInt32("SubjectId");
        return View(new SubjectDetailViewModel
        {
            Subject = ViewModelMapper.ToViewModel(subject.Subject),
            Documents = subject.Documents.Select(ViewModelMapper.ToViewModel).ToList(),
            CanCreateSubject = DocumentPermissions.CanManageSubjects(roleId),
            CanUploadDocument = DocumentPermissions.CanUploadToSubject(roleId, userSubjectId, id)
        });
    }

    [HttpGet]
    [RequireTeacher]
    public IActionResult Create()
    {
        return View(new SubjectCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireTeacher]
    public async Task<IActionResult> Create(SubjectCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var created = await _subjectService.CreateSubjectAsync(model.Code!, model.Name!, model.Description);
            await _appHub.Clients.All.SendAsync("CourseCreated", ViewModelMapper.ToListItemViewModel(created));
            TempData["Success"] = $"Created subject {created.Subject.Code} - {created.Subject.Name}.";
            return RedirectToAction(nameof(Manage));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    [RequireTeacher]
    public async Task<IActionResult> Edit(int id)
    {
        var subject = await _subjectService.GetSubjectAsync(id);
        if (subject is null)
            return NotFound();

        return View(new SubjectEditViewModel
        {
            Id = subject.Subject.Id,
            Code = subject.Subject.Code,
            Name = subject.Subject.Name,
            Description = subject.Subject.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireTeacher]
    public async Task<IActionResult> Edit(int id, SubjectEditViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var updated = await _subjectService.UpdateSubjectAsync(id, model.Code!, model.Name!, model.Description);
            await _appHub.Clients.All.SendAsync("CourseUpdated", ViewModelMapper.ToListItemViewModel(updated));
            TempData["Success"] = $"Updated subject {model.Code} successfully.";
            return RedirectToAction(nameof(Manage));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireTeacher]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _subjectService.DeleteSubjectAsync(id);
        if (success)
        {
            await _appHub.Clients.All.SendAsync("CourseDeleted", id);
            TempData["Success"] = "Deleted subject.";
        }
        else
        {
            TempData["Error"] = error ?? "Error deleting subject.";
        }

        return RedirectToAction(nameof(Manage));
    }

    [HttpPost("/api/subjects")]
    public async Task<IActionResult> CreateApi([FromBody] SubjectCreateViewModel model)
    {
        if (!CanManageSubjects())
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var created = await _subjectService.CreateSubjectAsync(model.Code!, model.Name!, model.Description);
            await _appHub.Clients.All.SendAsync("CourseCreated", ViewModelMapper.ToListItemViewModel(created));
            return CreatedAtAction(nameof(Details), new { id = created.Subject.Id }, ViewModelMapper.ToViewModel(created));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool CanViewSubjects()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        return roleId is not null && DocumentPermissions.CanView(roleId.Value);
    }

    private bool CanManageSubjects()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        return roleId is not null && DocumentPermissions.CanManageSubjects(roleId.Value);
    }
}
