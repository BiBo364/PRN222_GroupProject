# Hướng dẫn onboarding và phát triển

## 1. Mục tiêu

Tài liệu giúp developer mới bắt đầu làm việc an toàn.

Sau khi hoàn thành onboarding, developer có thể:

- Build solution.
- Chạy web.
- Kết nối database.
- Tìm đúng layer.
- Thêm use case.
- Viết test plan.
- Review thay đổi.
- Không commit secret.

## 2. Đọc trước

Thứ tự:

1. `docs/README.md`.
2. `ARCHITECTURE_HANDBOOK.md`.
3. `DATABASE_REFERENCE.md`.
4. Tài liệu domain liên quan.
5. `SECURITY_AND_HTTP_GUIDE.md`.
6. `TESTING_AND_QA_PLAYBOOK.md`.

## 3. Tooling

Khuyến nghị:

- Git.
- .NET 8 SDK.
- Visual Studio 2022 hoặc IDE tương thích.
- SQL Server.
- SSMS.
- Browser hiện đại.
- Node/npm cho Playwright CLI nếu cần.

## 4. Clone

```powershell
git clone <repository-url>
Set-Location PRN222_GroupProject
```

Không dùng URL hoặc token cá nhân trong tài liệu.

## 5. Khảo sát repository

Kiểm tra:

```powershell
git status
```

Liệt kê solution:

```powershell
Get-ChildItem
```

Tìm source:

```powershell
rg --files
```

Tìm symbol:

```powershell
rg -n "LearningService"
```

## 6. Cấu hình local

Tạo appsettings local từ template.

Điền:

- Connection string.
- AI config.
- SMTP nếu test.
- MoMo sandbox nếu test.

Không commit local secret.

Kiểm tra `.gitignore`.

## 7. Database

Khởi tạo theo script hiện có.

Sau đó chạy application để schema bổ sung được đồng bộ theo cơ chế hiện tại.

Kiểm tra:

- Database name.
- Tables.
- Seed roles.
- Seed subjects nếu có.
- Learning tables.
- Audit table.

## 8. Restore và build

```powershell
dotnet restore Assignmet1.sln
dotnet build Assignmet1.sln
```

Nếu build fail:

- Đọc error đầu.
- Không sửa ngẫu nhiên.
- Kiểm tra SDK.
- Kiểm tra package.

## 9. Run

```powershell
dotnet run --project Assignmet1_Presentation
```

Sau start:

- Mở login.
- Kiểm tra `/health/live`.
- Kiểm tra `/health/ready`.

## 10. First-day walkthrough

Thực hiện:

1. Login student.
2. Xem Home.
3. Xem Documents.
4. Xem Learning.
5. Logout.
6. Login lecturer.
7. Mở Question Bank.
8. Mở Manual Editor.
9. Mở Analytics.
10. Mở Audit.
11. Logout.
12. Login admin.
13. Mở Admin.
14. Mở Audit.

Không thay dữ liệu production.

## 11. Code navigation

### 11.1. Từ page

Ví dụ Analytics:

1. `Pages/Learning/Analytics.cshtml`.
2. `Analytics.cshtml.cs`.
3. `ILearningService`.
4. `LearningService`.
5. `ILearningRepository`.
6. `LearningRepository`.
7. `RagEduContext.Learning`.

### 11.2. Từ entity

1. Entity class.
2. Fluent mapping.
3. Repository query.
4. DTO.
5. Service rule.
6. UI.

### 11.3. Từ lỗi HTTP

1. Route.
2. Filter.
3. Handler.
4. Service.
5. Repository.
6. Exception handler.
7. Log trace.

## 12. Chọn layer cho thay đổi

### Presentation

Đặt ở đây nếu là:

- Markup.
- Page binding.
- Redirect.
- TempData.
- UI validation message.
- Browser script.

### Service

Đặt ở đây nếu là:

- Business rule.
- Authorization resource.
- Calculation.
- AI orchestration.
- Transactional use case.
- DTO mapping.

### Repository

Đặt ở đây nếu là:

- EF query.
- Include/projection.
- Persistence.
- Aggregate query.

## 13. Thêm một page mới

Checklist:

- [ ] Xác định route.
- [ ] Xác định role.
- [ ] Thêm PageModel.
- [ ] Inject service interface.
- [ ] GET handler.
- [ ] POST handler nếu cần.
- [ ] Anti-forgery.
- [ ] Validation.
- [ ] Empty state.
- [ ] Error state.
- [ ] Responsive.
- [ ] Navigation.
- [ ] Test.

## 14. Thêm một service method

Checklist:

- [ ] Tên theo use case.
- [ ] Input DTO.
- [ ] Output DTO.
- [ ] CancellationToken.
- [ ] Authorization.
- [ ] Validation.
- [ ] Repository methods.
- [ ] Error behavior.
- [ ] Audit/version.
- [ ] Tests.

## 15. Thêm entity

Checklist:

- [ ] Class.
- [ ] DbSet.
- [ ] Table mapping.
- [ ] Column mapping.
- [ ] Required/max length.
- [ ] Index.
- [ ] Foreign key.
- [ ] Delete behavior.
- [ ] Migration/synchronizer.
- [ ] Repository.
- [ ] Documentation.

## 16. Thêm repository query

Ưu tiên:

- Tên rõ.
- Filter ở SQL.
- Projection.
- AsNoTracking read-only.
- Pagination.
- CancellationToken.

Tránh:

- Load all.
- Client-side filter.
- N+1.
- Include dư.

## 17. Thêm AI use case

Checklist:

- [ ] Abstraction.
- [ ] Provider client.
- [ ] Options.
- [ ] Secret.
- [ ] Input cap.
- [ ] Subject scope.
- [ ] Prompt.
- [ ] Structured output.
- [ ] Parser.
- [ ] Validator.
- [ ] Draft.
- [ ] Rate limit.
- [ ] Error mapping.
- [ ] Tests.

## 18. Naming

### Class

PascalCase.

Ví dụ:

- `LearningSetVersion`.
- `AuditLogService`.

### Method

Động từ và đối tượng.

Ví dụ:

- `GetQuizAnalyticsAsync`.
- `RestoreQuizVersionAsync`.

### Async

Method awaitable có hậu tố `Async`.

### DTO

Tên biểu đạt use case:

- `QuizAnalyticsDashboardDto`.
- `QuizVersionHistoryDto`.

### Boolean

Dùng:

- `IsPublished`.
- `IsDeleted`.
- `CanRestore`.

## 19. Clean code

### 19.1. Method

Method nên:

- Một mức trừu tượng.
- Guard clause.
- Tên rõ.
- Không boolean parameter mơ hồ nếu tránh được.

### 19.2. Comment

Comment giải thích:

- Vì sao.
- Invariant.
- Workaround.

Không comment:

- Lặp code.
- Code đã rõ.

### 19.3. Constants

Business value quan trọng:

- Đặt constant.
- Hoặc options.
- Có test.

### 19.4. Null

Xử lý null ở boundary.

Không dùng null-forgiving bừa.

## 20. Validation

Validation nhiều lớp:

- View model cho format.
- Service cho business rule.
- Database cho integrity.

Ví dụ Quiz:

- UI: input required.
- Service: correct answer thuộc options.
- DB: foreign key và unique index.

## 21. Error handling

Không catch để bỏ qua.

Catch khi:

- Có fallback.
- Có mapping.
- Có cleanup.
- Có context log.

Unexpected exception để centralized handler xử lý.

## 22. Logging

Dùng structured log.

Tốt:

```text
Failed to restore learning set {LearningSetId} from version {VersionId}
```

Không tốt:

```text
Error: <concatenated everything>
```

Không log secret.

## 23. Authorization

Mỗi write use case hỏi:

- User là ai?
- Role gì?
- Resource của ai?
- Subject nào?
- State cho phép không?

Không chỉ dựa UI.

## 24. Transactions

Dùng transaction khi nhiều write phải all-or-nothing.

Ví dụ:

- Restore version.
- Submit attempt.
- Complete payment.

Không giữ transaction trong khi gọi AI ngoài mạng.

## 25. Git workflow

### 25.1. Branch

Tên có ý nghĩa:

```text
feature/quiz-analytics
fix/admin-modal-layer
docs/technical-handbook
```

### 25.2. Commit

Commit:

- Một mục đích.
- Message rõ.
- Không secret.
- Không artifact.

### 25.3. Status

Trước commit:

```powershell
git status
git diff --check
```

### 25.4. Review diff

Xem:

- File ngoài scope.
- Generated artifact.
- Password.
- Debug code.
- Test data.

## 26. Pull request

PR description:

### Bối cảnh

Vấn đề gì.

### Giải pháp

Thay đổi gì.

### Data

Schema/migration.

### Security

Role/ownership.

### Test

Lệnh và kết quả.

### Screenshot

Nếu UI.

### Rollback

Cách quay lại.

## 27. Code review checklist

- [ ] Scope rõ.
- [ ] Layer đúng.
- [ ] Authorization.
- [ ] Validation.
- [ ] Data integrity.
- [ ] Async/cancellation.
- [ ] Error handling.
- [ ] Logging.
- [ ] No secret.
- [ ] No N+1.
- [ ] Version/audit.
- [ ] UI states.
- [ ] Vietnamese.
- [ ] Tests.
- [ ] Docs.

## 28. Debugging workflow

### Reproduce

Ghi role, route, input.

### Observe

- Browser console.
- Network.
- Server log.
- Trace ID.
- Database rows.

### Isolate

- UI.
- Filter.
- Handler.
- Service.
- Repository.
- Dependency.

### Fix

Sửa nguyên nhân.

### Verify

- Original scenario.
- Negative path.
- Regression.

## 29. Browser testing

Dùng Playwright CLI khi cần:

- Navigation.
- Snapshot.
- Click.
- Fill.
- Screenshot.
- Console.
- Resize.

Quy trình:

1. Open.
2. Snapshot.
3. Dùng ref.
4. Interact.
5. Snapshot lại.
6. Kiểm tra console.

Xóa artifact test sau khi xong nếu không cần commit.

## 30. Database testing

Không dùng production DB.

Nếu cần data fixture:

- Prefix.
- Transaction.
- Cleanup.
- Verify.

Không hardcode credential trong script commit.

## 31. Common pitfalls

### Pitfall 1

Dùng ActionFilter cho Razor Page.

Hậu quả:

Filter không chạy như kỳ vọng.

### Pitfall 2

Modal nằm trong stacking context thấp.

Hậu quả:

Backdrop che.

### Pitfall 3

CSP thiếu font source.

Hậu quả:

Icon/font bị chặn.

### Pitfall 4

Xóa question đã có answer.

Hậu quả:

Mất lịch sử hoặc FK fail.

### Pitfall 5

Analytics bỏ option zero.

Hậu quả:

Phân tích sai.

### Pitfall 6

Student visibility lọc SubjectId.

Hậu quả:

Sai business rule.

### Pitfall 7

UI hiển thị provider.

Hậu quả:

Coupling sản phẩm với vendor.

## 32. First contribution exercise

Bài tập:

1. Chọn một message tiếng Việt thiếu dấu.
2. Sửa.
3. Build.
4. Mở browser.
5. Kiểm tra mobile.
6. Kiểm tra console.
7. Viết commit.

Không chọn migration cho đóng góp đầu tiên.

## 33. Definition of done

- [ ] Code build.
- [ ] Test.
- [ ] Diff clean.
- [ ] Không secret.
- [ ] Không test data.
- [ ] Quyền đúng.
- [ ] Data cũ giữ.
- [ ] UI responsive.
- [ ] Console clean.
- [ ] Docs cập nhật.

## 34. Kết luận

Developer mới nên ưu tiên hiểu luồng dữ liệu trước khi sửa.

Mọi request đi qua:

- Presentation.
- Service.
- Repository.

Mọi thay đổi quan trọng cần cân nhắc:

- Authorization.
- History.
- Audit.
- Version.
- Test.

Khi không chắc, tìm use case tương tự đang hoạt động và đối chiếu handbook.
