using System.ComponentModel.DataAnnotations;

namespace Assignmet1_Presentation.Models;

public class ChangePasswordViewModel
{
    [Display(Name = "Mat khau hien tai")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap mat khau moi.")]
    [MinLength(6, ErrorMessage = "Mat khau moi phai co it nhat 6 ky tu.")]
    [Display(Name = "Mat khau moi")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long xac nhan mat khau moi.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Xac nhan mat khau khong khop.")]
    [Display(Name = "Xac nhan mat khau moi")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool IsForcedChange { get; set; }
}
