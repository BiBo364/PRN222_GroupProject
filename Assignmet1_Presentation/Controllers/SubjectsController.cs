using Assignmet1_Presentation.Filters;
using Assignmet1_Presentation.Helpers;
using Assignmet1_Presentation.Mappings;
using Assignmet1_Presentation.Models;
using Assignment1_Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Assignmet1_Presentation.Controllers;

[RequireLogin]
public class SubjectsController : Controller
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
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
            TempData["Success"] = $"Created subject {created.Subject.Code} — {created.Subject.Name}.";
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
            await _subjectService.UpdateSubjectAsync(id, model.Code!, model.Name!, model.Description);
            TempData["Success"] = $"Cập nhật môn học {model.Code} thành công.";
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
            TempData["Success"] = "Đã xóa môn học.";
        else
            TempData["Error"] = error ?? "Lỗi khi xóa môn học.";

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