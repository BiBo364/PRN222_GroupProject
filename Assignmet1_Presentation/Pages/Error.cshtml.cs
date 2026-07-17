using System.Diagnostics;
using Assignmet1_Presentation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Assignmet1_Presentation.Pages;

// @page "/Error"
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PageModel
{
    public ErrorViewModel ErrorViewModel { get; private set; } = new();
    public int StatusCodeValue { get; private set; } = StatusCodes.Status500InternalServerError;
    public string Heading { get; private set; } = "Đã xảy ra lỗi trong quá trình xử lý.";
    public string Description { get; private set; } =
        "Bạn có thể thử tải lại trang hoặc quay về trang chủ.";

    public void OnGet(int? statusCode, string? requestId)
    {
        StatusCodeValue = statusCode ?? StatusCodes.Status500InternalServerError;
        (Heading, Description) = StatusCodeValue switch
        {
            StatusCodes.Status400BadRequest => (
                "Yêu cầu chưa hợp lệ.",
                "Vui lòng kiểm tra lại dữ liệu đã nhập và thử thực hiện thao tác một lần nữa."),
            StatusCodes.Status403Forbidden => (
                "Bạn không có quyền truy cập nội dung này.",
                "Hãy sử dụng tài khoản có quyền phù hợp hoặc quay về trang chủ."),
            StatusCodes.Status404NotFound => (
                "Không tìm thấy nội dung được yêu cầu.",
                "Nội dung có thể đã được di chuyển, xóa hoặc đường dẫn không còn chính xác."),
            StatusCodes.Status429TooManyRequests => (
                "Bạn đang thao tác quá nhanh.",
                "Vui lòng chờ một phút trước khi gửi yêu cầu tiếp theo."),
            _ => (
                "Đã xảy ra lỗi trong quá trình xử lý.",
                "Bạn có thể thử tải lại trang hoặc quay về trang chủ. Nếu lỗi tiếp tục xuất hiện, hãy cung cấp mã yêu cầu cho quản trị viên.")
        };
        ErrorViewModel = new ErrorViewModel
        {
            RequestId = requestId ?? Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };
    }
}
