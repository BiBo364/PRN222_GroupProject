# Cẩm nang bảo mật và HTTP

## 1. Mục tiêu

Tài liệu này mô tả các lớp bảo vệ của RAG Edu Hub.

Phạm vi:

- Xác thực.
- Authorization.
- Session.
- Razor Page filter.
- Resource ownership.
- CSRF.
- XSS.
- CSP.
- Rate limiting.
- Error handling.
- Secret management.
- Upload security.
- AI security.
- Audit.
- Privacy.

Tài liệu không thay thế security review chuyên sâu.

## 2. Threat model tóm tắt

Các tài sản cần bảo vệ:

- Tài khoản.
- Tài liệu môn học.
- Nội dung chat.
- Câu hỏi và đáp án.
- Kết quả sinh viên.
- Audit log.
- Dữ liệu thanh toán.
- API key.
- Connection string.

Các actor rủi ro:

- Người dùng chưa đăng nhập.
- Student cố truy cập staff page.
- Lecturer truy cập môn không thuộc quyền.
- Tài khoản bị chiếm.
- Script tự động.
- File độc hại.
- Prompt injection trong tài liệu.
- Callback payment giả.
- Insider có quyền.

## 3. Security principles

### 3.1. Deny by default

Trang nhạy cảm phải yêu cầu quyền rõ.

Không dựa vào việc menu bị ẩn.

URL trực tiếp vẫn phải bị chặn.

POST trực tiếp vẫn phải bị chặn.

### 3.2. Server authoritative

Server quyết định:

- User identity.
- Role.
- Ownership.
- Score.
- Publish state.
- Payment result.
- Audit actor.

Client chỉ gửi input.

### 3.3. Least privilege

Mỗi role chỉ có quyền cần thiết.

Giảng viên không xem audit của giảng viên khác.

Sinh viên không xem analytics.

Admin access không tự mở rộng nếu chưa có use case.

### 3.4. Defense in depth

Một request quan trọng có thể được bảo vệ bởi:

- Authentication filter.
- Role filter.
- Resource check trong service.
- Anti-forgery token.
- Validation.
- Rate limit.
- Audit.

## 4. Authentication

### 4.1. Login

Login nhận username hoặc email và password.

Server phải:

- Normalize định danh.
- Tìm user.
- Xác minh password.
- Kiểm tra active.
- Tạo session.
- Ghi login log.

Thông báo login không nên tiết lộ:

- Username có tồn tại.
- Email có tồn tại.
- Tài khoản cụ thể bị khóa vì lý do nội bộ.

### 4.2. Password

Password phải:

- Không log.
- Không lưu plain text.
- Không gửi trong query string.
- Không đưa vào TempData.
- Không trả lại view.

Khuyến nghị:

- ASP.NET Core PasswordHasher.
- Salt riêng.
- Work factor phù hợp.
- Upgrade hash khi login.

### 4.3. Forced password change

Tài khoản import có thể buộc đổi mật khẩu.

Filter toàn cục:

- Cho vào trang đổi mật khẩu.
- Cho logout.
- Redirect các trang khác.

Không chỉ ẩn menu.

### 4.4. Logout

Logout phải:

- Clear session.
- Xóa cookie session nếu phù hợp.
- Redirect an toàn.
- Không dùng GET cho thao tác nhạy cảm nếu có rủi ro CSRF.

## 5. Session security

### 5.1. Dữ liệu session

Session chứa identifier tối thiểu.

Không lưu:

- Password.
- API key.
- Full payment payload.
- Tài liệu lớn.

### 5.2. Cookie

Production nên cấu hình:

- HttpOnly.
- Secure.
- SameSite phù hợp.
- Expiration.
- Essential theo policy.

### 5.3. Session fixation

Sau login nên rotate session identifier nếu framework/config yêu cầu.

Không nhận session ID từ query.

### 5.4. Session expiration

Khi session hết:

- Filter redirect login.
- Không render dữ liệu nhạy cảm.
- POST không chạy.
- Autosave xử lý 401/redirect rõ.

## 6. Razor Pages authorization

### 6.1. Vì sao dùng `IPageFilter`

Razor Pages chạy page handler.

Filter cần triển khai đúng pipeline Razor Pages.

`ActionFilterAttribute` chủ yếu dành cho MVC action.

Nếu dùng sai:

- Page có thể render dù UI nghĩ đã bảo vệ.
- Session hết hạn có thể để lộ dữ liệu.

### 6.2. `RequireLoginAttribute`

Trách nhiệm:

- Kiểm tra User ID trong session.
- Redirect login khi thiếu.

Không chịu trách nhiệm:

- Kiểm tra role.
- Kiểm tra ownership.

### 6.3. `RequireAdminAttribute`

Trách nhiệm:

- Yêu cầu login.
- Yêu cầu admin.
- Redirect an toàn khi sai role.

### 6.4. `RequireLecturerAttribute`

Trách nhiệm:

- Yêu cầu login.
- Yêu cầu lecturer.

Dashboard và version history dùng filter này.

### 6.5. `RequireAuditRoleAttribute`

Cho phép:

- Admin.
- Lecturer.

Service tiếp tục giới hạn:

- Admin thấy toàn hệ thống.
- Lecturer chỉ thấy log của mình.

### 6.6. Document filters

Upload:

- Lecturer.
- Có subject assignment.

Delete:

- Lecturer.
- Ownership kiểm tra ở handler/service.

## 7. Resource authorization

### 7.1. Quiz ownership

Khi lecturer gửi set ID:

Server phải kiểm tra:

- Set tồn tại.
- Set chưa deleted nếu use case yêu cầu.
- Creator/subject khớp lecturer.

Không chỉ kiểm tra role lecturer.

### 7.2. Version ownership

Restore version phải kiểm tra:

- Version thuộc set.
- Set thuộc lecturer.
- Version ID không được ghép với set ID khác.

### 7.3. Attempt ownership

Student chỉ xem:

- Attempt có `user_id` của mình.

Lecturer analytics chỉ xem:

- Subject thuộc quyền.

### 7.4. Document ownership

Document action cần:

- Subject match.
- Role match.
- Document tồn tại.

## 8. CSRF

### 8.1. Form POST

Razor Pages hỗ trợ anti-forgery token.

Mọi mutating form phải giữ token.

Không disable validation toàn cục.

### 8.2. AJAX

AJAX POST phải gửi token.

Token có thể lấy từ hidden input hoặc meta theo convention.

### 8.3. Callback ngoại lệ

Payment IPN không dùng browser anti-forgery.

Nó phải dùng:

- Signature verification.
- Amount verification.
- Order verification.
- Idempotency.

Không mở anti-forgery exception rộng hơn route cần thiết.

## 9. XSS

### 9.1. Razor encoding

Razor encode output mặc định.

Không dùng `Html.Raw` cho nội dung người dùng nếu chưa sanitize.

### 9.2. AI output

AI output là untrusted.

Không render raw HTML.

Nếu hỗ trợ Markdown:

- Dùng parser an toàn.
- Sanitize HTML.
- Disable dangerous URL.

### 9.3. Document content

Text trích từ document là untrusted.

Không chèn trực tiếp vào script.

Không dùng làm HTML.

## 10. Content Security Policy

### 10.1. Mục đích

CSP giảm rủi ro XSS và resource injection.

### 10.2. Nguồn hiện dùng

Layout dùng một số CDN cho:

- Font Awesome.
- SignalR.
- Bootstrap hoặc thư viện hỗ trợ.
- Google Fonts.

CSP phải allow đúng:

- Style source.
- Script source.
- Font source.
- Connect source.

### 10.3. Nguyên tắc

- Chỉ allow domain thực sự dùng.
- Tránh wildcard.
- Hạn chế `unsafe-inline`.
- Dùng nonce/hash khi cải tiến.
- Kiểm tra console browser.

### 10.4. Regression

Sau thay CSP:

- Mở login.
- Mở trang có icon.
- Mở chat SignalR.
- Mở modal.
- Kiểm tra console.
- Kiểm tra font.

## 11. Security headers

Các header nên cân nhắc:

- Content-Security-Policy.
- X-Content-Type-Options.
- Referrer-Policy.
- Permissions-Policy.
- Strict-Transport-Security.
- Frame-ancestors qua CSP.

HSTS chỉ bật đúng trong HTTPS production.

## 12. Rate limiting

### 12.1. Global

Global limiter giảm request burst.

Partition:

- Theo user nếu có session.
- Theo IP nếu anonymous.

### 12.2. AI policy

AI generation có giới hạn riêng.

Thiết kế hiện tại:

- 20 request.
- Trong 5 phút.

Mục tiêu:

- Kiểm soát chi phí.
- Chống spam.
- Bảo vệ provider.

### 12.3. Quiz submission policy

Submit limiter dùng sliding window.

Mục tiêu:

- Chống double submit.
- Chống flood.
- Không cản thao tác bình thường.

### 12.4. HTTP 429

Response phải:

- Status 429.
- Có thông báo tiếng Việt.
- Có Retry-After nếu phù hợp.
- API trả JSON.
- Browser có trang/thông báo rõ.

### 12.5. Không dùng limiter thay authorization

Limiter không xác định quyền.

Request trong quota vẫn phải qua authorization.

## 13. Centralized exception handling

### 13.1. Mục tiêu

- Không leak stack trace.
- Thông báo nhất quán.
- Trace được request.
- API và HTML phù hợp.

### 13.2. Mapping

Các status thường dùng:

- 400 cho request invalid.
- 403 cho forbidden.
- 404 cho not found.
- 429 cho rate limit.
- 500 cho lỗi không dự kiến.

### 13.3. Trace identifier

Trace ID:

- Hiển thị trên trang lỗi.
- Ghi trong log.
- Dùng khi support.

Không xem trace ID là secret.

### 13.4. Problem Details

API error nên theo cấu trúc Problem Details.

Không trả HTML cho API client.

Không trả raw exception message nếu chứa nội bộ.

## 14. Secret management

### 14.1. Secret gồm

- SQL password.
- API key AI.
- MoMo secret.
- SMTP password.
- Token signing key.

### 14.2. Không commit

Không commit:

- `appsettings.json` có secret.
- `.env` thật.
- User secrets file.
- Certificate private key.

### 14.3. Template

`appsettings.example.json`:

- Chứa key name.
- Chứa placeholder.
- Không chứa secret thật.
- Có comment qua docs, vì JSON không hỗ trợ comment chuẩn.

### 14.4. Local

Dùng:

- User Secrets.
- Environment variable.
- Local untracked config.

### 14.5. Production

Dùng:

- Secret manager.
- Environment injection.
- Managed identity khi có.
- Rotation.

## 15. Upload security

### 15.1. Allowlist

Chỉ nhận extension được hỗ trợ:

- PDF.
- DOCX.
- PPTX.

### 15.2. Không tin extension

Nên kiểm tra:

- Content type.
- File signature.
- Parser behavior.

### 15.3. Filename

- Chuẩn hóa.
- Không dùng làm storage path trực tiếp.
- Ngăn path traversal.
- Giữ original name chỉ để hiển thị.

### 15.4. Size

Giới hạn:

- Request body.
- File.
- Page.
- Extracted text.

### 15.5. Storage

File upload:

- Không thực thi.
- Không nằm trong đường dẫn cho phép chạy script.
- Tên vật lý ngẫu nhiên.
- Quyền filesystem tối thiểu.

### 15.6. Parser

Parser có thể bị:

- Zip bomb.
- Corrupt file.
- Huge XML.
- Image bomb.

Cần timeout và size cap.

## 16. AI security

### 16.1. Prompt injection

Tài liệu có thể chứa:

“Bỏ qua hướng dẫn trước”.

Đó là dữ liệu.

Không được coi là system instruction.

### 16.2. Data minimization

Chỉ gửi chunk cần thiết.

Không gửi toàn DB.

Không gửi student data cho generation.

### 16.3. Output validation

AI output không đáng tin.

Luôn:

- Parse.
- Validate.
- Normalize.
- Review.

### 16.4. Provider error

Không hiển thị raw response chứa:

- Request ID nội bộ.
- Endpoint.
- API key fragment.
- Billing detail.

### 16.5. Abuse

Rate limit.

Input length limit.

Subject scope.

Audit generation.

## 17. Payment security

### 17.1. Signature

IPN phải verify HMAC/signature theo provider.

### 17.2. Amount

Amount callback phải khớp ticket.

Không tin amount từ browser.

### 17.3. Order

Order ID và request ID phải khớp.

### 17.4. Idempotency

Callback lặp không cấp subscription lặp.

### 17.5. Logging

Không log secret key.

Payload có thể log có kiểm soát.

Áp dụng retention.

## 18. Audit security

### 18.1. Tamper resistance

UI không có CRUD audit.

Staff không sửa audit record.

### 18.2. Sensitive fields

Details JSON không chứa:

- Password.
- Token.
- Full payment secret.
- Full document text.

### 18.3. Viewer access

Admin:

- Toàn hệ thống.

Lecturer:

- Actor là chính mình.

Student:

- Không truy cập.

### 18.4. Availability

Lỗi audit không nên làm crash main action theo thiết kế hiện tại.

Nhưng phải có application log.

## 19. Privacy

### 19.1. Personal data

Có thể gồm:

- Tên.
- Email.
- IP.
- User agent.
- Kết quả học.
- Nội dung chat.

### 19.2. Minimization

Chỉ thu thập cần thiết.

Không dùng test data thật.

Không copy production DB sang máy cá nhân không kiểm soát.

### 19.3. Retention

Cần policy cho:

- Login log.
- Audit log.
- Chat.
- Attempt.
- Payment.
- Deleted document.

## 20. Security testing checklist

### 20.1. Anonymous

- [ ] Truy cập Admin bị redirect.
- [ ] Truy cập Audit bị redirect.
- [ ] Truy cập Analytics bị redirect.
- [ ] Truy cập Version bị redirect.
- [ ] POST trực tiếp bị chặn.

### 20.2. Student

- [ ] Không vào Admin.
- [ ] Không vào Audit.
- [ ] Không vào Analytics.
- [ ] Không vào Question Bank.
- [ ] Không sửa Quiz.
- [ ] Làm published Quiz được.

### 20.3. Lecturer

- [ ] Không vào Admin.
- [ ] Xem Audit của mình.
- [ ] Không xem Audit người khác.
- [ ] Chỉ sửa Quiz của mình.
- [ ] Không restore version của người khác.
- [ ] Chỉ upload đúng subject.

### 20.4. Admin

- [ ] Vào quản lý user.
- [ ] Xem audit toàn hệ thống.
- [ ] Không tự vô hiệu hóa sai nếu rule cấm.
- [ ] Mutating action có confirm.

### 20.5. HTTP

- [ ] CSRF token thiếu bị chặn.
- [ ] 429 đúng.
- [ ] 404 không leak.
- [ ] 500 có trace.
- [ ] CSP không có console error.
- [ ] Security headers tồn tại.

### 20.6. Input

- [ ] Script tag được encode.
- [ ] Filename traversal bị chặn.
- [ ] File quá lớn bị chặn.
- [ ] Invalid AI JSON không lưu.
- [ ] Over-post role không tác dụng.

## 21. Incident severity gợi ý

### Severity 1

- Lộ secret production.
- Bypass admin.
- Thay đổi điểm diện rộng.
- Payment cấp quyền sai diện rộng.

### Severity 2

- Lecturer xem dữ liệu lecturer khác.
- Student xem answer không nên thấy.
- Upload độc hại có ảnh hưởng.
- Audit ngừng ghi.

### Severity 3

- Rate limit sai.
- CSP chặn icon.
- Error page thiếu trace.
- Session timeout UX kém.

## 22. Security definition of done

- [ ] Authentication đúng.
- [ ] Role đúng.
- [ ] Resource ownership đúng.
- [ ] CSRF đúng.
- [ ] Input validation server.
- [ ] Output encoding.
- [ ] Secret không xuất hiện.
- [ ] Rate limit nếu tốn tài nguyên.
- [ ] Audit nếu thao tác quan trọng.
- [ ] Error không leak.
- [ ] Test URL trực tiếp.
- [ ] Test session hết hạn.
- [ ] Test wrong role.
- [ ] Test mobile không làm mất action bảo mật.

## 23. Kết luận

Bảo mật không phải một filter duy nhất.

Nó là chuỗi kiểm soát từ request đến dữ liệu.

Các điểm quan trọng nhất:

- Đúng filter cho Razor Pages.
- Ownership trong service.
- Score ở server.
- AI output được validate.
- Secret ở ngoài Git.
- Callback payment được verify.
- Audit có scope.
- Error có trace nhưng không leak.
