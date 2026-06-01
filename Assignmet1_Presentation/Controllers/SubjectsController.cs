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

    public async Task<IActionResult> Details(int id)
    {
        if (!CanViewSubjects())
            return RedirectToAction("Login", "Account");

        var subject = await _subjectService.GetSubjectAsync(id);
        if (subject is null)
            return NotFound();

        var roleId = HttpContext.Session.GetInt32("RoleId")!.Value;
        return View(new SubjectDetailViewModel
        {
            Subject = ViewModelMapper.ToViewModel(subject.Subject),
            Documents = subject.Documents.Select(ViewModelMapper.ToViewModel).ToList(),
            CanCreateSubject = DocumentPermissions.CanUpload(roleId),
            CanUploadDocument = DocumentPermissions.CanUpload(roleId)
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!CanManageSubjects())
            return RedirectToAction("Index", "Documents");

        return View(new SubjectCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubjectCreateViewModel model)
    {
        if (!CanManageSubjects())
            return RedirectToAction("Index", "Documents");

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var created = await _subjectService.CreateSubjectAsync(model.Code!, model.Name!, model.Description);
            TempData["Success"] = $"Created subject {created.Subject.Code} — {created.Subject.Name}.";
            return RedirectToAction(nameof(Details), new { id = created.Subject.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
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
        return roleId is not null && DocumentPermissions.CanUpload(roleId.Value);
    }
}