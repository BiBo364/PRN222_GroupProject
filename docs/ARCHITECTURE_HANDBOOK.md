# Cẩm nang kiến trúc RAG Edu Hub

## 1. Executive summary

RAG Edu Hub là ứng dụng ASP.NET Core .NET 8 sử dụng Razor Pages.

Hệ thống được chia thành ba project theo kiến trúc phân lớp:

```text
Browser
  |
  v
Assignmet1_Presentation
  |
  v
Assignment1_Service
  |
  v
Assignment1_Repository
  |
  v
SQL Server
```

Presentation chịu trách nhiệm HTTP và trải nghiệm người dùng.

Service chịu trách nhiệm business rule và orchestration.

Repository chịu trách nhiệm persistence.

Dependency phải đi từ ngoài vào trong theo chiều:

```text
Presentation -> Service -> Repository
```

Repository không tham chiếu Presentation.

Service không tham chiếu Razor Pages.

Razor Page không nên viết SQL.

Entity không nên chứa logic giao diện.

## 2. Bối cảnh nghiệp vụ

Hệ thống giải quyết các nhu cầu:

- Quản lý học liệu theo môn học.
- Cho phép giảng viên tải tài liệu.
- Trích xuất và index nội dung.
- Chat dựa trên tài liệu.
- Tạo nội dung ôn tập bằng AI.
- Tạo Quiz thủ công.
- Tái sử dụng ngân hàng câu hỏi.
- Cho mọi sinh viên truy cập Quiz đã phát hành.
- Chấm điểm và lưu lịch sử.
- Phân tích chất lượng câu hỏi.
- Khôi phục phiên bản Quiz.
- Theo dõi thao tác của giảng viên và quản trị viên.
- Quản lý gói học và thanh toán.

## 3. Các actor

### 3.1. Quản trị viên

Quản trị viên chịu trách nhiệm:

- Quản lý người dùng.
- Kích hoạt hoặc vô hiệu hóa tài khoản.
- Phân công môn học cho giảng viên.
- Xem nhật ký toàn hệ thống.
- Xem báo cáo thanh toán.
- Quản lý các chức năng quản trị khác.

Quản trị viên không mặc nhiên thay thế giảng viên trong mọi use case tạo học liệu.

Quyền cụ thể phải được kiểm tra tại server.

### 3.2. Giảng viên

Giảng viên chịu trách nhiệm:

- Quản lý tài liệu thuộc môn được phân công.
- Tạo câu hỏi bằng AI.
- Tạo câu hỏi thủ công.
- Xây dựng Quiz.
- Phát hành hoặc thu hồi Quiz.
- Xem dashboard kết quả.
- Xem phân tích câu hỏi.
- Khôi phục phiên bản.
- Xem audit log của chính mình.

### 3.3. Sinh viên

Sinh viên có thể:

- Truy cập tài liệu được phép.
- Chat AI theo quy định gói học.
- Xem Quiz đã phát hành.
- Làm mọi Quiz đã phát hành mà không cần được phân công vào môn học.
- Xem kết quả của mình.
- Sử dụng hoạt động ôn tập.

Sinh viên không được:

- Tạo hoặc sửa Quiz.
- Xem dashboard giảng viên.
- Xem version history.
- Xem audit log.
- Quản lý người dùng.

## 4. Solution structure

### 4.1. Assignment1_Repository

Nhóm thư mục chính:

```text
Assignment1_Repository/
  Basic/
  Models/
  Repositories/
    Interfaces/
```

`Models` chứa entity ánh xạ database.

`RagEduContext` là EF Core DbContext chính.

`RagEduContext.Learning.cs` mở rộng model configuration cho miền ôn tập.

`Repositories` triển khai truy vấn.

`Repositories/Interfaces` định nghĩa hợp đồng persistence.

### 4.2. Assignment1_Service

Nhóm thư mục chính:

```text
Assignment1_Service/
  Helpers/
  Infrastructure/
  Models/
  Services/
    Interfaces/
```

`Models` ở project này chủ yếu là DTO và options.

`Services` triển khai use case.

`Helpers` chứa thuật toán thuần hoặc tiện ích.

`Infrastructure` chứa tác vụ liên quan schema hoặc bootstrapping.

`DependencyInjection.cs` là composition helper của layer Service/Repository.

### 4.3. Assignmet1_Presentation

Nhóm thư mục chính:

```text
Assignmet1_Presentation/
  Endpoints/
  Filters/
  Helpers/
  Infrastructure/
  Models/
  Pages/
  wwwroot/
```

`Pages` chứa Razor Page và PageModel.

`Models` chứa view model dành cho Presentation.

`Filters` kiểm tra đăng nhập, role và yêu cầu đổi mật khẩu.

`Infrastructure` chứa middleware, exception handler và health check.

`wwwroot` chứa CSS, JavaScript và static asset.

`Program.cs` là composition root của web app.

## 5. Dependency rules

### 5.1. Quy tắc bắt buộc

- Presentation có thể dùng interface Service.
- Service có thể dùng interface Repository.
- Repository có thể dùng EF Core.
- Service không được trả entity trực tiếp cho UI nếu DTO phù hợp hơn.
- Repository không được tạo HTML.
- PageModel không được tự gọi SQL.
- JavaScript không được chứa business rule duy nhất.
- Validation quan trọng phải tồn tại ở server.

### 5.2. Quy tắc khuyến nghị

- Mỗi use case phức tạp có request DTO.
- Mỗi query UI có response DTO tối ưu.
- Repository tập trung vào dữ liệu.
- Service tập trung vào ý nghĩa nghiệp vụ.
- PageModel ngắn và dễ đọc.
- Handler POST trả redirect sau thành công.
- CancellationToken được truyền xuyên suốt.
- Thời gian được chuẩn hóa nhất quán.

### 5.3. Dấu hiệu vi phạm layer

Các dấu hiệu cần review:

- `DbContext` xuất hiện trong PageModel.
- `HttpContext` xuất hiện trong Repository.
- `TempData` xuất hiện trong Service.
- Entity chứa CSS class.
- Repository trả `IActionResult`.
- JavaScript quyết định điểm số cuối cùng.
- UI tự quyết định role mà server không kiểm tra.
- Service phụ thuộc selector HTML.

## 6. Request lifecycle

### 6.1. GET request thông thường

Luồng khái quát:

1. Kestrel nhận request.
2. Security header middleware thêm header.
3. Static file middleware xử lý tài nguyên tĩnh nếu phù hợp.
4. Routing xác định endpoint.
5. Session middleware khôi phục session.
6. Rate limiter kiểm tra quota.
7. Page filter kiểm tra đăng nhập và role.
8. PageModel gọi service.
9. Service gọi repository.
10. Repository truy vấn DbContext.
11. DTO quay trở lại Presentation.
12. Razor render HTML.
13. Response trả về trình duyệt.

### 6.2. POST request thông thường

Luồng khái quát:

1. Browser gửi form kèm anti-forgery token.
2. Server xác thực token.
3. Filter xác thực session và role.
4. Model binding tạo input model.
5. Model validation chạy.
6. PageModel kiểm tra lỗi.
7. Service thực hiện use case.
8. Repository ghi database.
9. Audit middleware quan sát kết quả.
10. Handler redirect theo Post/Redirect/Get.
11. Browser tải trang kết quả.

### 6.3. Request lỗi

Khi exception chưa được xử lý:

1. `GlobalExceptionHandler` nhận exception.
2. Handler phân loại status code.
3. Trace identifier được giữ lại.
4. API nhận Problem Details.
5. Request HTML được chuyển đến trang Error.
6. Log server chứa thông tin kỹ thuật.
7. Người dùng nhận thông điệp tiếng Việt an toàn.

Không hiển thị stack trace cho người dùng production.

## 7. Composition root

`Program.cs` chịu trách nhiệm:

- Tạo builder.
- Đăng ký Razor Pages.
- Cấu hình filter toàn cục.
- Đăng ký session.
- Đăng ký service của application.
- Đăng ký exception handler.
- Đăng ký Problem Details.
- Đăng ký health checks.
- Đăng ký rate limiting.
- Đăng ký options.
- Xây middleware pipeline.
- Map Razor Pages.
- Map API.
- Map health endpoints.

Không nên đặt business rule vào `Program.cs`.

`Program.cs` chỉ nên cấu hình và kết nối thành phần.

## 8. Domain map

### 8.1. Identity domain

Entity chính:

- `User`.
- `Role`.
- `Permission`.
- `LoginLog`.
- `RefreshToken`.

Trách nhiệm:

- Xác thực tài khoản.
- Lưu role.
- Theo dõi trạng thái active.
- Ghi lần đăng nhập.
- Buộc đổi mật khẩu khi cần.

### 8.2. Academic domain

Entity chính:

- `Subject`.
- `Chapter`.

Trách nhiệm:

- Tổ chức nội dung theo môn.
- Phân công giảng viên.
- Liên kết tài liệu.
- Liên kết câu hỏi.
- Liên kết Quiz.

### 8.3. Document domain

Entity chính:

- `Document`.
- `Chunk`.
- `ChunkingConfig`.
- `Embedding`.
- `EmbeddingModel`.

Trách nhiệm:

- Upload.
- Trích xuất.
- Chunking.
- Embedding.
- Indexing.
- Soft delete.
- Restore.

### 8.4. Chat domain

Entity chính:

- `Session`.
- `Message`.
- `MessageCitation`.
- `StudentChatUsage`.

Trách nhiệm:

- Hội thoại theo người dùng.
- Hội thoại theo môn.
- Lưu câu hỏi và trả lời.
- Trích dẫn nguồn.
- Kiểm soát quota.

### 8.5. Learning domain

Entity chính:

- `QuestionBankItem`.
- `LearningSet`.
- `LearningSetItem`.
- `LearningAttempt`.
- `LearningAttemptAnswer`.
- `LearningSetVersion`.

Trách nhiệm:

- Ngân hàng câu hỏi.
- Quiz và hoạt động ôn tập.
- Quan hệ câu hỏi trong Quiz.
- Lượt làm.
- Câu trả lời đã chọn.
- Snapshot phiên bản.

### 8.6. Governance domain

Entity chính:

- `AuditLog`.

Trách nhiệm:

- Truy vết thao tác ghi.
- Xác định actor.
- Xác định role.
- Xác định action.
- Xác định entity.
- Ghi request path và status.
- Hỗ trợ điều tra.

### 8.7. Commerce domain

Entity chính:

- `SubscriptionPlan`.
- `PaymentTicket`.
- `UserSubscription`.

Trách nhiệm:

- Danh mục gói.
- Thanh toán.
- Trạng thái giao dịch.
- Kích hoạt quyền sử dụng.
- Báo cáo.

## 9. Service responsibilities

### 9.1. UserServices

Chịu trách nhiệm các use case tài khoản và quản trị người dùng.

Không nên để PageModel trực tiếp chỉnh entity User.

### 9.2. SubjectService

Chịu trách nhiệm môn học và các rule liên quan.

Mọi check quyền sở hữu môn cần nhất quán.

### 9.3. DocumentService

Chịu trách nhiệm vòng đời tài liệu.

Service điều phối:

- File validation.
- Persistence.
- Text extraction.
- Chunking.
- Embedding.
- Index status.

### 9.4. ChatService

Chịu trách nhiệm:

- Xác định session.
- Kiểm tra quota.
- Truy xuất chunk.
- Xây prompt.
- Gọi AI.
- Lưu message.
- Lưu citation.

### 9.5. LearningService

Đây là service trung tâm của miền ôn tập.

Service xử lý:

- Lấy learning hub.
- Lấy ngân hàng câu hỏi.
- Tạo nội dung thủ công.
- Tạo nội dung bằng AI.
- Lưu Quiz.
- Phát hành.
- Thu hồi.
- Xóa mềm.
- Khôi phục.
- Tạo lượt làm.
- Chấm điểm.
- Dashboard.
- Phân tích câu hỏi.
- Phiên bản.
- Khôi phục phiên bản.

Khi service lớn dần, có thể tách theo use case trong tương lai.

Không tách chỉ để giảm số dòng nếu làm mất transaction boundary hoặc tăng coupling.

### 9.6. AuditLogService

Chịu trách nhiệm query nhật ký theo quyền.

Middleware chịu trách nhiệm capture request mutating.

Service chịu trách nhiệm lọc và trình bày dữ liệu.

### 9.7. SubscriptionService

Chịu trách nhiệm:

- Lấy gói.
- Kiểm tra subscription.
- Tính thời hạn.
- Hoàn tất payment ticket.
- Cấp quyền sử dụng.

## 10. Repository responsibilities

Repository nên:

- Biểu đạt query có ý nghĩa.
- Dùng `AsNoTracking` cho read-only query.
- Include quan hệ cần thiết.
- Không trả dữ liệu dư thừa nếu có query tối ưu.
- Tôn trọng CancellationToken.
- Không swallow database exception.
- Không quyết định message UI.

Repository không nên:

- Kiểm tra session.
- Tạo TempData.
- Gọi AI.
- Gửi email.
- Render view.
- Định dạng tiền cho UI.

## 11. DTO strategy

### 11.1. Vì sao dùng DTO

DTO giúp:

- Không làm lộ entity.
- Kiểm soát trường trả về.
- Tối ưu query.
- Tách schema UI khỏi schema database.
- Giảm over-posting.
- Dễ version hóa hợp đồng.

### 11.2. Input DTO

Input DTO phải:

- Chỉ chứa trường người dùng được phép nhập.
- Có validation phù hợp.
- Không cho client tự gửi owner ID nếu owner lấy từ session.
- Không cho client tự đặt audit actor.
- Không tin điểm số từ client.

### 11.3. Output DTO

Output DTO phải:

- Chứa dữ liệu UI cần.
- Có giá trị mặc định rõ.
- Tránh lazy loading.
- Tránh navigation graph lớn.
- Dùng tên dễ hiểu.

## 12. Authorization architecture

### 12.1. Session keys

Presentation sử dụng session để giữ các giá trị nhận diện.

Các giá trị thường gặp:

- User ID.
- Role ID.
- Subject ID.
- Tên hiển thị.
- Cờ buộc đổi mật khẩu.

Session chỉ là nguồn nhận diện ở Presentation.

Business rule nhạy cảm vẫn phải kiểm tra dữ liệu hiện tại.

### 12.2. Page filters

Các filter hiện có:

- `RequireLoginAttribute`.
- `RequireAdminAttribute`.
- `RequireLecturerAttribute`.
- `RequireAuditRoleAttribute`.
- `RequireDocumentUploadAttribute`.
- `RequireDocumentDeleteAttribute`.
- `EnforcePasswordChangeAttribute`.

Các filter triển khai `IPageFilter`.

Điều này phù hợp Razor Pages.

Không dùng `ActionFilterAttribute` cho PageModel nếu mong chặn page handler.

### 12.3. Resource authorization

Role check không đủ cho mọi trường hợp.

Ví dụ:

- Giảng viên chỉ sửa Quiz do mình quản lý.
- Giảng viên chỉ upload vào môn được phân công.
- Sinh viên chỉ xem attempt của mình.
- Version restore phải thuộc Quiz của giảng viên.

Resource authorization nên nằm trong service.

## 13. AI integration architecture

### 13.1. Abstraction

UI dùng từ “AI”.

Service nên phụ thuộc abstraction như `IStudyContentAiService`.

Provider-specific client nằm phía sau abstraction.

Điều này giúp:

- Đổi provider.
- Mock khi test.
- Kiểm soát retry.
- Chuẩn hóa lỗi.
- Giữ UI độc lập.

### 13.2. Structured generation

AI generation nên yêu cầu output có cấu trúc.

Output phải được parse và validate.

Không lưu mù quáng text AI vào database.

Đối với câu hỏi trắc nghiệm:

- Phải có prompt.
- Phải có đúng bốn lựa chọn.
- Phải có đáp án đúng thuộc danh sách.
- Nên có giải thích.
- Nên có độ khó.
- Nên có nguồn tham chiếu.

### 13.3. Human-in-the-loop

AI tạo bản nháp.

Giảng viên review.

Giảng viên chỉnh sửa.

Giảng viên phát hành.

Không nên tự động phát hành ngay sau generation.

## 14. Versioning architecture

### 14.1. Snapshot

Mỗi phiên bản Quiz lưu snapshot JSON bất biến.

Snapshot giữ:

- Metadata của bộ.
- Cấu hình.
- Danh sách câu hỏi.
- Thứ tự.
- Điểm.
- Trạng thái tại thời điểm lưu.

### 14.2. Restore

Quy trình restore an toàn:

1. Xác thực giảng viên.
2. Xác thực quyền sở hữu Quiz.
3. Tìm version.
4. Sao lưu trạng thái hiện tại.
5. Parse snapshot.
6. Validate snapshot.
7. Áp dụng dữ liệu.
8. Đặt trạng thái bản nháp.
9. Tạo version mới mô tả restore.
10. Ghi audit log.

### 14.3. Bảo toàn attempt cũ

Attempt cũ phải tiếp tục tham chiếu được câu hỏi lịch sử.

Không hard delete câu hỏi đã có answer nếu điều đó phá foreign key hoặc lịch sử.

Khi thay đổi câu hỏi, cần cân nhắc tạo bản ghi mới thay vì sửa nghĩa của câu hỏi cũ.

## 15. Analytics architecture

### 15.1. Grain

Các grain chính:

- Một attempt.
- Một answer.
- Một question.
- Một Quiz.
- Một student.
- Một ngày.

Không trộn grain khi tính metric.

### 15.2. Metric chính

Dashboard hiện có thể tính:

- Tổng lượt làm.
- Sinh viên duy nhất.
- Điểm trung bình.
- Tỷ lệ đạt.
- Thời gian trung bình.
- Xu hướng theo ngày.
- Hiệu quả theo Quiz.
- Tỷ lệ đúng theo câu.
- Tỷ lệ chọn từng đáp án.

### 15.3. Phân loại độ khó

Phân loại dựa trên tỷ lệ trả lời đúng thực tế.

Ngưỡng hiện tại:

- Dễ khi tỷ lệ đúng từ 80%.
- Trung bình khi tỷ lệ đúng từ 50% đến dưới 80%.
- Khó khi tỷ lệ đúng dưới 50%.

Ngưỡng là business rule.

Nếu thay đổi, phải cập nhật tài liệu và test.

### 15.4. Zero-selection option

Mọi đáp án cấu hình phải xuất hiện trong phân tích.

Đáp án không ai chọn vẫn hiển thị 0%.

Nếu bỏ đáp án 0%, giảng viên có thể hiểu sai distractor coverage.

## 16. Audit architecture

### 16.1. Phạm vi

Audit middleware quan sát các request ghi:

- POST.
- PUT.
- PATCH.
- DELETE.

### 16.2. Actor

Actor được lấy từ session server.

Client không được tự khai actor.

### 16.3. Nội dung

Audit log có thể lưu:

- User ID.
- Role ID.
- Action.
- Category.
- Entity type.
- Entity ID.
- Description.
- Details JSON.
- IP.
- User agent.
- Path.
- HTTP method.
- Status code.
- Trace identifier.
- Created time.

### 16.4. Failure isolation

Không nên để lỗi ghi audit làm hỏng thao tác chính của người dùng.

Tuy nhiên lỗi audit phải được log để vận hành biết.

Trong hệ thống yêu cầu compliance cao, có thể chọn chiến lược fail-closed.

Đó chưa phải mặc định hiện tại.

## 17. Resilience architecture

### 17.1. Rate limiting

Rate limiter bảo vệ:

- Tài nguyên AI tốn chi phí.
- Endpoint nộp bài.
- Toàn bộ ứng dụng trước request burst.

Partition ưu tiên user đã đăng nhập.

Nếu chưa đăng nhập, partition theo IP.

### 17.2. Health checks

`/health/live` xác nhận process còn sống.

`/health/ready` xác nhận application và database sẵn sàng.

Liveness không nên phụ thuộc dịch vụ ngoài dễ chập chờn.

Readiness có thể phụ thuộc database.

### 17.3. Centralized errors

Exception handler tập trung giúp:

- Thông điệp nhất quán.
- Giảm leak stack trace.
- Trả Problem Details cho API.
- Trả trang Error cho HTML.
- Gắn trace identifier.

## 18. Frontend architecture

### 18.1. Razor Pages

Mỗi page thường có:

- `.cshtml` cho markup.
- `.cshtml.cs` cho PageModel.

Handler phổ biến:

- `OnGetAsync`.
- `OnPostAsync`.
- `OnPostDeleteAsync`.
- `OnPostRestoreAsync`.
- `OnPostPublishAsync`.

### 18.2. Shared layout

Layout chịu trách nhiệm:

- Navigation.
- Account menu.
- Footer.
- Shared CSS.
- Shared script.
- Alert/notification host.

Modal nên được đặt đủ cao trong stacking context.

JavaScript hiện có cơ chế chuyển modal ra body để tránh bị che.

### 18.3. Progressive enhancement

Form quan trọng phải hoạt động ở server.

JavaScript cải thiện:

- Confirm dialog.
- Autosave.
- Dynamic question editor.
- Animation.
- Toast.

JavaScript không được là lớp bảo mật.

## 19. Data consistency

### 19.1. Transaction boundary

Use case nhiều bước nên cân nhắc transaction:

- Tạo Quiz và items.
- Restore version.
- Chấm attempt.
- Hoàn tất payment.
- Xóa chuỗi dữ liệu liên quan.

### 19.2. Idempotency

Các callback payment cần idempotent.

Autosave cần tránh tạo trùng.

Restore không được chạy hai lần do double submit mà không có dấu vết.

AI generation có thể tốn phí nên cần chống click lặp.

### 19.3. Concurrency

Rủi ro concurrency:

- Hai tab cùng sửa Quiz.
- Hai request cùng tạo version number.
- Double-submit bài.
- Hai callback payment.
- Hai admin cùng đổi trạng thái user.

Khuyến nghị tương lai:

- Row version.
- Concurrency token.
- Idempotency key.
- Unique index.
- Transaction phù hợp.

## 20. Time handling

Thời gian trong database cần nhất quán.

Khuyến nghị:

- Lưu UTC.
- Chuyển timezone khi hiển thị.
- Không gọi `ToLocalTime` nếu Kind không đáng tin.
- Test qua ranh giới ngày.
- Test daylight saving nếu triển khai đa quốc gia.

Ứng dụng hiện phục vụ ngữ cảnh Việt Nam.

Hiển thị nên dùng định dạng ngày giờ Việt Nam.

## 21. Logging

Log kỹ thuật nên có:

- Trace identifier.
- Event name.
- Entity ID.
- User ID khi an toàn.
- Duration.
- Status.
- Exception.

Log không nên có:

- Password.
- API key.
- Full connection string.
- Token.
- Nội dung thanh toán nhạy cảm.
- Toàn bộ tài liệu riêng tư.

Audit log và application log có mục đích khác nhau.

Audit trả lời “ai đã làm gì”.

Application log trả lời “hệ thống đã chạy thế nào”.

## 22. Extensibility

### 22.1. Thêm loại câu hỏi

Cần thay đổi:

- Constants.
- DTO.
- Validator.
- Manual editor.
- AI schema.
- Answer evaluator.
- Analytics.
- Version snapshot.
- Test.

### 22.2. Thêm AI provider

Cần:

- Provider client.
- Options.
- DI registration.
- Adapter qua abstraction.
- Retry policy.
- Error mapping.
- Health/monitoring nếu cần.
- Secret management.
- Integration test.

### 22.3. Thêm activity type

Cần:

- Activity constant.
- Compose workflow.
- Player UI.
- Result model nếu có.
- Versioning.
- Analytics.
- Authorization.
- Test.

## 23. Anti-patterns

### 23.1. Fat PageModel

PageModel không nên:

- Tính thống kê phức tạp.
- Parse AI JSON.
- Thực hiện nhiều query.
- Xử lý transaction.

### 23.2. Generic repository lạm dụng

Generic repository hữu ích cho CRUD đơn giản.

Query nghiệp vụ phức tạp nên có repository method rõ nghĩa.

Không ép mọi query vào `GetAll`.

### 23.3. Hard delete dữ liệu lịch sử

Không hard delete:

- Attempt.
- Answer.
- Version.
- Audit.

trừ khi có retention policy và quy trình được duyệt.

### 23.4. Client-authoritative score

Client không được gửi tổng điểm được tin cậy.

Server phải chấm lại từ question và answer.

### 23.5. Role-only authorization

Role giảng viên chưa đủ để sửa mọi Quiz.

Phải kiểm tra ownership hoặc subject scope.

## 24. Architectural decision checklist

Trước khi thêm tính năng:

- [ ] Use case thuộc domain nào?
- [ ] Actor nào được dùng?
- [ ] Dữ liệu nào thay đổi?
- [ ] Cần transaction không?
- [ ] Cần audit không?
- [ ] Cần versioning không?
- [ ] Có dữ liệu lịch sử cần bảo toàn không?
- [ ] Có tốn AI quota không?
- [ ] Có cần rate limit riêng không?
- [ ] Có cần health dependency mới không?
- [ ] Có route API hay Razor Page?
- [ ] Có input validation server không?
- [ ] Có resource authorization không?
- [ ] Có empty state không?
- [ ] Có error state không?
- [ ] Có mobile layout không?
- [ ] Có test case không?
- [ ] Có rollback plan không?

## 25. Definition of done về kiến trúc

Một thay đổi được coi là hoàn tất khi:

- Build thành công.
- Dependency direction đúng.
- Business rule nằm ở Service.
- Query nằm ở Repository.
- UI không bypass authorization.
- Dữ liệu cũ không bị phá.
- Có validation.
- Có error handling.
- Có audit nếu là thao tác quan trọng.
- Có version nếu cần khôi phục.
- Có test phù hợp.
- Có tài liệu cập nhật.
- Không có secret.
- Không có nội dung tiếng Việt lỗi dấu.

## 26. Kết luận

Kiến trúc hiện tại phù hợp một ứng dụng Razor Pages phân lớp.

Điểm quan trọng nhất không phải số lượng project.

Điểm quan trọng là giữ đúng trách nhiệm của từng layer.

Mọi tính năng AI, Quiz, dashboard và audit đều phải bảo toàn:

- Quyền truy cập.
- Tính toàn vẹn dữ liệu.
- Khả năng truy vết.
- Khả năng khôi phục.
- Trải nghiệm người dùng.

Khi hệ thống lớn hơn, có thể tách service theo use case.

Việc tách chỉ nên thực hiện khi có lợi ích rõ về ownership, testability hoặc deployment.
