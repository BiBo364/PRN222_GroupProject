# Playbook kiểm thử và đảm bảo chất lượng

## 1. Mục tiêu

Playbook định nghĩa cách kiểm thử RAG Edu Hub theo rủi ro.

Phạm vi:

- Unit test.
- Integration test.
- Page test.
- Browser test.
- Security test.
- Data test.
- Resilience test.
- Accessibility test.
- Responsive test.
- Regression test.

Tài liệu cũng cung cấp test case thủ công có thể dùng trong nghiệm thu.

## 2. Testing principles

### 2.1. Test hành vi

Test phải mô tả điều người dùng hoặc hệ thống quan sát được.

Không khóa test vào implementation không cần thiết.

### 2.2. Test server rule

Validation UI không đủ.

Mọi rule quan trọng phải test ở server.

### 2.3. Test negative path

Mỗi happy path cần ít nhất một negative path.

Ví dụ:

- Đúng role.
- Sai role.
- Hết session.
- Resource không thuộc quyền.
- Input invalid.
- Dependency fail.

### 2.4. Test data độc lập

Test data:

- Có prefix.
- Không dùng dữ liệu thật.
- Có cleanup.
- Không phụ thuộc thứ tự test.

### 2.5. Bằng chứng

Test report nên có:

- Build/version.
- Môi trường.
- Ngày giờ.
- Test data.
- Expected.
- Actual.
- Screenshot khi cần.
- Trace ID khi lỗi.

## 3. Test pyramid

### 3.1. Unit test

Phù hợp:

- Answer normalization.
- Score calculation.
- Difficulty classification.
- Percentage.
- Snapshot validation.
- Audit classification.
- Rate partition key helper.

### 3.2. Integration test

Phù hợp:

- Repository query.
- EF relationship.
- Service với test database.
- Version restore transaction.
- Submit attempt.
- Analytics aggregate.

### 3.3. Page test

Phù hợp:

- Handler result.
- Model validation.
- Redirect.
- TempData.
- Filter.

### 3.4. Browser test

Phù hợp:

- Modal.
- Dropdown.
- Autosave.
- Responsive.
- Navigation.
- Error display.
- Full user flow.

## 4. Test environments

### 4.1. Local

Dùng cho:

- Developer feedback.
- Debug.
- Browser exploratory.

### 4.2. Integration

Dùng:

- Test database riêng.
- Secret test.
- Provider mock hoặc sandbox.

### 4.3. Staging

Gần production:

- HTTPS.
- Reverse proxy.
- Production-like SQL.
- Sandbox payment.
- Controlled AI quota.

### 4.4. Production verification

Chỉ smoke không phá dữ liệu.

Không chạy bulk test.

Không chạy rate flood tùy tiện.

## 5. Test data strategy

Tạo tối thiểu:

- Một admin.
- Hai lecturer.
- Hai subject.
- Ba student không subject.
- Một document indexed.
- Một document error.
- Một question bank.
- Một draft Quiz.
- Một published Quiz.
- Một deleted Quiz.
- Attempts với phân phối đáp án.
- Hai versions.
- Audit records.

### 5.1. Naming

Prefix:

```text
qa_
```

Email:

```text
qa.<scenario>@example.test
```

### 5.2. Cleanup

Cleanup theo thứ tự khóa ngoại.

Luôn:

- Xác định row count.
- Dùng transaction.
- Chỉ xóa prefix test.
- Verify còn 0.

## 6. Build verification

### TC-BUILD-001: Restore solution

Tiền điều kiện:

- SDK có sẵn.

Bước:

1. Chạy `dotnet restore`.

Kỳ vọng:

- Exit code 0.
- Không thiếu package.

### TC-BUILD-002: Build solution

Bước:

1. Chạy `dotnet build`.

Kỳ vọng:

- Exit code 0.
- Không compile error.

### TC-BUILD-003: Diff check

Bước:

1. Chạy `git diff --check`.

Kỳ vọng:

- Không whitespace error.

### TC-BUILD-004: Secret scan thủ công

Bước:

1. Kiểm tra staged diff.
2. Tìm password/key pattern.

Kỳ vọng:

- Không có secret.

## 7. Authentication test cases

### TC-AUTH-001: Login đúng username

Tiền điều kiện:

- User active.

Bước:

1. Mở login.
2. Nhập username.
3. Nhập password đúng.
4. Submit.

Kỳ vọng:

- Redirect Home.
- Header hiện user.
- Session được tạo.

### TC-AUTH-002: Login đúng email

Bước:

1. Nhập email.
2. Nhập password đúng.

Kỳ vọng:

- Login thành công.

### TC-AUTH-003: Password sai

Kỳ vọng:

- Không login.
- Thông báo chung.
- Không lộ user tồn tại.

### TC-AUTH-004: User inactive

Kỳ vọng:

- Không login.
- Session không tạo.

### TC-AUTH-005: Input rỗng

Kỳ vọng:

- Validation tiếng Việt.
- Không gọi service không cần thiết.

### TC-AUTH-006: Logout

Kỳ vọng:

- Session clear.
- Trang protected redirect login.

### TC-AUTH-007: Session hết hạn

Bước:

1. Login.
2. Mở Admin.
3. Làm session hết hạn.
4. Reload.

Kỳ vọng:

- Redirect login.
- Không render bảng user.

### TC-AUTH-008: Forced password change

Kỳ vọng:

- Các trang khác redirect ChangePassword.
- ChangePassword truy cập được.
- Logout truy cập được.

## 8. Authorization matrix tests

### TC-RBAC-001: Anonymous vào Admin

Kỳ vọng:

- Redirect login.

### TC-RBAC-002: Student vào Admin

Kỳ vọng:

- Redirect Home hoặc forbidden theo thiết kế.

### TC-RBAC-003: Lecturer vào Admin

Kỳ vọng:

- Bị chặn.

### TC-RBAC-004: Admin vào Admin

Kỳ vọng:

- Thành công.

### TC-RBAC-005: Anonymous vào Audit

Kỳ vọng:

- Redirect login.

### TC-RBAC-006: Student vào Audit

Kỳ vọng:

- Bị chặn.

### TC-RBAC-007: Lecturer vào Audit

Kỳ vọng:

- Thành công.
- Chỉ log actor lecturer.

### TC-RBAC-008: Admin vào Audit

Kỳ vọng:

- Thành công.
- Log toàn hệ thống.

### TC-RBAC-009: Student vào Analytics

Kỳ vọng:

- Bị chặn.

### TC-RBAC-010: Lecturer vào Analytics

Kỳ vọng:

- Thành công đúng subject.

### TC-RBAC-011: Lecturer A mở Quiz của B

Kỳ vọng:

- Not found/forbidden.
- Không lộ dữ liệu.

### TC-RBAC-012: Lecturer A restore version của B

Kỳ vọng:

- Bị chặn.
- Không tạo version.

## 9. Admin user management

### TC-ADMIN-001: Search username

Kỳ vọng:

- Trả đúng user.
- Giữ filter.

### TC-ADMIN-002: Filter role

Kỳ vọng:

- Chỉ user đúng role.

### TC-ADMIN-003: Filter subject

Kỳ vọng:

- Chỉ user đúng subject theo rule.

### TC-ADMIN-004: Disable user

Bước:

1. Click Vô hiệu.
2. Modal mở.
3. Confirm.

Kỳ vọng:

- Modal không bị backdrop che.
- User inactive.
- Success message.
- Audit record.

### TC-ADMIN-005: Cancel disable

Kỳ vọng:

- Không đổi trạng thái.

### TC-ADMIN-006: Enable user

Kỳ vọng:

- User active.
- Audit record.

### TC-ADMIN-007: Assign lecturer subject

Kỳ vọng:

- Subject update.
- Lecturer session mới nhận đúng.
- Audit.

### TC-ADMIN-008: Pagination

Kỳ vọng:

- Count đúng.
- Link giữ filter.
- Không trùng row.

## 10. Subject test cases

### TC-SUBJECT-001: Create valid

Kỳ vọng:

- Subject tạo.
- Code/name đúng.

### TC-SUBJECT-002: Duplicate code

Kỳ vọng:

- Validation.
- Không duplicate.

### TC-SUBJECT-003: Edit

Kỳ vọng:

- Data update.
- Existing relation giữ.

### TC-SUBJECT-004: Soft delete

Kỳ vọng:

- Ẩn mặc định.
- Xuất hiện recycle.

### TC-SUBJECT-005: Restore

Kỳ vọng:

- Trở lại danh sách.

### TC-SUBJECT-006: Student không có subject

Kỳ vọng:

- Vẫn làm published Quiz.

## 11. Document upload tests

### TC-DOC-001: Lecturer đúng subject upload PDF

Kỳ vọng:

- Accepted.
- Status processing rồi indexed.
- Chunk tạo.

### TC-DOC-002: Upload DOCX

Kỳ vọng:

- Text extract.
- Chunk tạo.

### TC-DOC-003: Upload PPTX

Kỳ vọng:

- Slide extract.
- Image xử lý theo thiết kế.

### TC-DOC-004: Student upload

Kỳ vọng:

- Bị chặn server.

### TC-DOC-005: Lecturer chưa có subject

Kỳ vọng:

- Bị chặn.
- Thông báo tiếng Việt.

### TC-DOC-006: Extension không hỗ trợ

Kỳ vọng:

- Reject.
- Không lưu file.

### TC-DOC-007: Duplicate filename

Kỳ vọng:

- Xử lý theo rule.
- Không overwrite.

### TC-DOC-008: Duplicate hash

Kỳ vọng:

- Phát hiện.
- Không index trùng ngoài ý muốn.

### TC-DOC-009: File corrupt

Kỳ vọng:

- Status error.
- Error an toàn.

### TC-DOC-010: File quá lớn

Kỳ vọng:

- Reject sớm.

### TC-DOC-011: Path traversal filename

Kỳ vọng:

- Filename normalize.
- Không ghi ngoài storage.

### TC-DOC-012: Soft delete

Kỳ vọng:

- Ẩn.
- Chunks giữ theo policy.

### TC-DOC-013: Restore

Kỳ vọng:

- Hiển thị lại.

### TC-DOC-014: Re-index

Kỳ vọng:

- Không duplicate chunk.
- Status đúng.

## 12. Chat RAG tests

### TC-CHAT-001: Tạo session

Kỳ vọng:

- Session user đúng.
- Subject đúng.

### TC-CHAT-002: Gửi câu hỏi có nguồn

Kỳ vọng:

- Answer tiếng Việt.
- Citation.
- Message lưu.

### TC-CHAT-003: Câu hỏi không đủ context

Kỳ vọng:

- Không bịa.
- Fallback phù hợp.

### TC-CHAT-004: Session người khác

Kỳ vọng:

- Bị chặn.

### TC-CHAT-005: Subject isolation

Kỳ vọng:

- Không citation môn khác.

### TC-CHAT-006: AI timeout

Kỳ vọng:

- Error rõ.
- User message không mất ngoài thiết kế.

### TC-CHAT-007: Quota free

Kỳ vọng:

- Đếm đúng.
- Vượt quota bị chặn.

### TC-CHAT-008: Prompt injection trong document

Kỳ vọng:

- Không làm theo lệnh tài liệu.
- Answer dựa nội dung.

## 13. AI question generation tests

### TC-AI-001: Generate multiple choice

Kỳ vọng:

- Đúng số câu.
- Mỗi câu bốn option.
- Một correct answer.

### TC-AI-002: Generate true/false

Kỳ vọng:

- Type đúng.
- Correct answer hợp lệ.

### TC-AI-003: Generate từ document thuộc quyền

Kỳ vọng:

- Thành công.
- Source reference.

### TC-AI-004: Document môn khác

Kỳ vọng:

- Bị chặn trước provider.

### TC-AI-005: Invalid count

Kỳ vọng:

- Validation.

### TC-AI-006: Provider JSON invalid

Kỳ vọng:

- Không lưu garbage.
- Error.

### TC-AI-007: Provider trả 3 options

Kỳ vọng:

- Validator reject hoặc repair có kiểm soát.
- Không publish.

### TC-AI-008: Correct answer không thuộc options

Kỳ vọng:

- Reject.

### TC-AI-009: Duplicate options

Kỳ vọng:

- Reject.

### TC-AI-010: Rate request 1-20

Kỳ vọng:

- Đi qua limiter theo window hiện tại.

### TC-AI-011: Request 21

Kỳ vọng:

- HTTP 429.

### TC-AI-012: UI wording

Kỳ vọng:

- “AI”.
- Không hiện provider ở CTA.

## 14. Question bank tests

### TC-QB-001: Create manual MCQ

Kỳ vọng:

- Save.
- Bốn option.

### TC-QB-002: Prompt empty

Kỳ vọng:

- Validation.

### TC-QB-003: Option empty

Kỳ vọng:

- Validation.

### TC-QB-004: Correct mismatch

Kỳ vọng:

- Validation.

### TC-QB-005: Search

Kỳ vọng:

- Match prompt/topic.

### TC-QB-006: Filter difficulty

Kỳ vọng:

- Đúng rows.

### TC-QB-007: Filter AI/manual

Kỳ vọng:

- Đúng provenance.

### TC-QB-008: Deactivate

Kỳ vọng:

- Không chọn mới mặc định.
- Attempt cũ giữ.

### TC-QB-009: Reuse in two Quiz

Kỳ vọng:

- Được phép.
- Không duplicate entity ngoài ý muốn.

### TC-QB-010: Duplicate in same Quiz

Kỳ vọng:

- Bị chặn.

## 15. Manual editor tests

### TC-EDIT-001: Create draft

Kỳ vọng:

- Draft ID.
- Không published.

### TC-EDIT-002: Add question

Kỳ vọng:

- UI row.
- Save server.

### TC-EDIT-003: Edit title

Kỳ vọng:

- Autosave.
- Saved status.

### TC-EDIT-004: Reorder

Kỳ vọng:

- Order giữ sau reload.

### TC-EDIT-005: Delete from set

Kỳ vọng:

- Item mất.
- Bank item theo rule vẫn còn.

### TC-EDIT-006: Duplicate question

Kỳ vọng:

- New question hoặc new item theo UX rõ.

### TC-EDIT-007: Offline edit

Kỳ vọng:

- Local draft.
- Indicator.

### TC-EDIT-008: Online again

Kỳ vọng:

- Đồng bộ.

### TC-EDIT-009: Close tab

Kỳ vọng:

- Mở lại thấy tiến trình.

### TC-EDIT-010: Two tabs

Kỳ vọng:

- Conflict không overwrite im lặng.

### TC-EDIT-011: Invalid MCQ

Kỳ vọng:

- Không publish.

### TC-EDIT-012: Server save fail

Kỳ vọng:

- Không báo saved.
- Local giữ.

## 16. Publish tests

### TC-PUB-001: Publish valid

Kỳ vọng:

- Published.
- Version.
- Audit.

### TC-PUB-002: Publish empty set

Kỳ vọng:

- Bị chặn.

### TC-PUB-003: Publish invalid question

Kỳ vọng:

- Bị chặn.
- Chỉ rõ câu.

### TC-PUB-004: Student thấy published

Kỳ vọng:

- Có.

### TC-PUB-005: Student không thấy draft

Kỳ vọng:

- Không.

### TC-PUB-006: Unpublish

Kỳ vọng:

- Attempt cũ giữ.
- Student không bắt đầu mới theo rule.
- Version.
- Audit.

## 17. Student Quiz visibility

### TC-VIS-001: Student SubjectId null

Kỳ vọng:

- Thấy mọi published Quiz.

### TC-VIS-002: Student gắn Subject A

Kỳ vọng:

- Vẫn thấy published Quiz Subject B.

### TC-VIS-003: Deleted published Quiz

Kỳ vọng:

- Không thấy.

### TC-VIS-004: Draft Quiz

Kỳ vọng:

- Không thấy.

### TC-VIS-005: Lecturer draft owner

Kỳ vọng:

- Thấy trong management.

## 18. Attempt and scoring tests

### TC-ATT-001: Start published

Kỳ vọng:

- Attempt tạo.

### TC-ATT-002: Start draft bằng URL

Kỳ vọng:

- Bị chặn.

### TC-ATT-003: Refresh

Kỳ vọng:

- Cùng attempt.
- Order giữ.

### TC-ATT-004: Shuffle question

Kỳ vọng:

- Order khác theo seed.
- Correct mapping giữ.

### TC-ATT-005: Shuffle options

Kỳ vọng:

- Options đổi.
- Correct chấm đúng.

### TC-ATT-006: All correct

Kỳ vọng:

- 100%.
- Correct count total.

### TC-ATT-007: All wrong

Kỳ vọng:

- 0%.

### TC-ATT-008: Partial

Kỳ vọng:

- Điểm theo points.

### TC-ATT-009: Missing answer

Kỳ vọng:

- 0 cho câu.
- Submit theo policy.

### TC-ATT-010: Tamper score

Kỳ vọng:

- Server bỏ qua client score.

### TC-ATT-011: Tamper correct flag

Kỳ vọng:

- Server chấm lại.

### TC-ATT-012: Double submit

Kỳ vọng:

- Một completion.

### TC-ATT-013: 31 submit nhanh

Kỳ vọng:

- Limiter chặn theo policy.

### TC-ATT-014: Duration

Kỳ vọng:

- Từ server timestamps.

## 19. Analytics fixture

Tạo:

- 3 students.
- 1 Quiz.
- 2 questions.
- 6 attempts.

Question A:

- 2 correct.
- 4 wrong.

Question B:

- 4 correct.
- 2 wrong.

Option B distribution:

- Correct: 4.
- Distractor 1: 1.
- Distractor 2: 1.
- Distractor 3: 0.

## 20. Analytics tests

### TC-AN-001: Total attempts

Kỳ vọng:

- 6.

### TC-AN-002: Unique students

Kỳ vọng:

- 3.

### TC-AN-003: Average

Với fixture phù hợp:

- 50%.

### TC-AN-004: Pass rate

Ngưỡng 50%.

Kỳ vọng fixture:

- 83.3% nếu 5/6 đạt.

### TC-AN-005: Average duration

Kỳ vọng theo fixture:

- Giá trị tính đúng.

### TC-AN-006: Duration >24h

Kỳ vọng:

- Loại khỏi average.

### TC-AN-007: Question A difficulty

Correct 33.3%.

Kỳ vọng:

- Khó.

### TC-AN-008: Question B difficulty

Correct 66.7%.

Kỳ vọng:

- Trung bình.

### TC-AN-009: Zero option

Kỳ vọng:

- Hiển thị 0 · 0%.

### TC-AN-010: 7-day filter

Kỳ vọng:

- Loại attempt cũ.

### TC-AN-011: 30-day zero dates

Kỳ vọng:

- Có bucket 0.

### TC-AN-012: No data

Kỳ vọng:

- Empty state.
- Không divide by zero.

### TC-AN-013: Quiz filter

Kỳ vọng:

- Chỉ selected Quiz.

### TC-AN-014: Subject ownership

Kỳ vọng:

- Lecturer không xem subject khác.

## 21. Version tests

### TC-VER-001: Existing Quiz no history

Kỳ vọng:

- Initial version tạo khi mở history.

### TC-VER-002: Version label

Kỳ vọng:

- `v1`.
- Không hiện literal template.

### TC-VER-003: Publish version

Kỳ vọng:

- Version number tăng.

### TC-VER-004: Manual save version

Kỳ vọng:

- Summary phù hợp.

### TC-VER-005: Autosave

Kỳ vọng:

- Không spam version theo policy.

### TC-VER-006: Restore v1

Kỳ vọng:

- Current backup.
- Apply v1.
- Set draft.
- New version.

### TC-VER-007: Restore confirm cancel

Kỳ vọng:

- Không đổi.

### TC-VER-008: Snapshot corrupt

Kỳ vọng:

- Rollback.

### TC-VER-009: Attempt preservation

Kỳ vọng:

- Count attempt không đổi.

### TC-VER-010: Audit

Kỳ vọng:

- Action restore.
- Entity ID.
- Status.

## 22. Recycle tests

### TC-REC-001: Soft delete Quiz

Kỳ vọng:

- Vào recycle.
- Student không thấy.

### TC-REC-002: Restore

Kỳ vọng:

- Draft.
- Student chưa thấy đến publish.

### TC-REC-003: Delete wrong owner

Kỳ vọng:

- Bị chặn.

### TC-REC-004: Restore wrong owner

Kỳ vọng:

- Bị chặn.

## 23. Audit tests

### TC-AUD-001: Lecturer restore

Kỳ vọng:

- Một record.
- Actor lecturer.
- Category learning.
- Action restore.
- POST path.

### TC-AUD-002: Admin disable user

Kỳ vọng:

- Record category user.

### TC-AUD-003: Lecturer viewer

Kỳ vọng:

- Chỉ own records.

### TC-AUD-004: Admin viewer

Kỳ vọng:

- All staff records.

### TC-AUD-005: Filter category

Kỳ vọng:

- Đúng.

### TC-AUD-006: Filter date

Kỳ vọng:

- Inclusive rule rõ.

### TC-AUD-007: Search entity

Kỳ vọng:

- Match ID/description.

### TC-AUD-008: Autosave excluded

Kỳ vọng:

- Không noise.

### TC-AUD-009: Audit write failure

Kỳ vọng:

- Main action theo policy vẫn thành công.
- Application log có lỗi.

## 24. Health tests

### TC-HC-001: Live healthy

Kỳ vọng:

- HTTP 200.
- Application check healthy.

### TC-HC-002: Ready healthy

Kỳ vọng:

- HTTP 200.
- Database healthy.

### TC-HC-003: Database down

Kỳ vọng:

- Live có thể 200.
- Ready unhealthy.

### TC-HC-004: JSON content

Kỳ vọng:

- Status.
- Duration.
- Checks.
- Không secret.

## 25. Error handling tests

### TC-ERR-001: Error 400

Kỳ vọng:

- Tiếng Việt.

### TC-ERR-002: Error 403

Kỳ vọng:

- Không leak.

### TC-ERR-003: Error 404

Kỳ vọng:

- Friendly.

### TC-ERR-004: Error 429

Kỳ vọng:

- Nói chờ/thử lại.

### TC-ERR-005: Error 500

Kỳ vọng:

- Trace ID.
- Không stack trace.

### TC-ERR-006: API exception

Kỳ vọng:

- Problem Details JSON.

### TC-ERR-007: HTML exception

Kỳ vọng:

- Error page.

## 26. CSP and console tests

### TC-CSP-001: Google Font

Kỳ vọng:

- Load.
- Không CSP error.

### TC-CSP-002: Font Awesome

Kỳ vọng:

- Icon load.
- Font allowed.

### TC-CSP-003: SignalR

Kỳ vọng:

- Script/connect allowed.

### TC-CSP-004: Inline attack

Kỳ vọng:

- Bị encode/chặn.

### TC-CSP-005: Console clean

Kỳ vọng:

- 0 error trong journey chính.

## 27. Responsive tests

Viewport:

- 390×844.
- 768×1024.
- 1366×768.
- 1920×1080.

### TC-RESP-001: Navigation mobile

Kỳ vọng:

- Menu button.
- Không che content.

### TC-RESP-002: Analytics mobile

Kỳ vọng:

- Filter một cột.
- Cards một cột.
- Không horizontal page overflow.

### TC-RESP-003: Table mobile

Kỳ vọng:

- Container scroll hoặc layout thay thế.
- Page không vỡ.

### TC-RESP-004: Modal mobile

Kỳ vọng:

- Nằm trong viewport.
- Confirm bấm được.

### TC-RESP-005: Account menu

Kỳ vọng:

- Logout nhìn thấy.
- Không bị hero che.

### TC-RESP-006: Editor mobile

Kỳ vọng:

- Fields dễ nhập.
- Sticky action không che.

## 28. Accessibility tests

### TC-A11Y-001: Keyboard navigation

Kỳ vọng:

- Tab order logic.

### TC-A11Y-002: Focus visible

Kỳ vọng:

- Mọi control có focus.

### TC-A11Y-003: Form labels

Kỳ vọng:

- Input có accessible name.

### TC-A11Y-004: Modal focus

Kỳ vọng:

- Focus vào modal.
- Escape đóng.
- Focus trả lại trigger.

### TC-A11Y-005: Color

Kỳ vọng:

- Không dùng màu duy nhất truyền status.

### TC-A11Y-006: Contrast

Kỳ vọng:

- Text đọc được.

### TC-A11Y-007: Chart

Kỳ vọng:

- Accessible label.
- Text metric.

### TC-A11Y-008: Icon button

Kỳ vọng:

- Aria label hoặc visible text.

## 29. Performance tests

### TC-PERF-001: Question bank 10k rows

Đo:

- Query duration.
- Page render.

Kỳ vọng:

- Pagination.

### TC-PERF-002: Analytics 100k attempts

Đo:

- SQL.
- Memory.
- Response.

### TC-PERF-003: Audit 1M rows

Đo:

- Filter.
- Pagination.
- Index.

### TC-PERF-004: Large Quiz

Đo:

- Editor.
- Snapshot size.
- Player.

### TC-PERF-005: AI concurrent

Đo:

- Limiter.
- Timeout.
- Provider quota.

## 30. Concurrency tests

### TC-CON-001: Two editor tabs

Kỳ vọng:

- Conflict detected hoặc deterministic.

### TC-CON-002: Two version creates

Kỳ vọng:

- Unique version.
- Retry/transaction.

### TC-CON-003: Double payment callback

Kỳ vọng:

- One subscription.

### TC-CON-004: Double submit

Kỳ vọng:

- One completion.

### TC-CON-005: Two admins toggle

Kỳ vọng:

- Final state rõ.
- Audit hai action.

## 31. Data integrity tests

### TC-DATA-001

Published set có item.

### TC-DATA-002

MCQ có bốn options.

### TC-DATA-003

Correct answer thuộc options.

### TC-DATA-004

Attempt score không vượt total.

### TC-DATA-005

Version number unique.

### TC-DATA-006

Set item order unique.

### TC-DATA-007

Answer question thuộc set/version hợp lệ.

### TC-DATA-008

Approved payment có subscription.

### TC-DATA-009

Embedding dimension đúng.

### TC-DATA-010

Audit time và actor hợp lệ.

## 32. Regression suite tối thiểu

Mỗi release:

- Login.
- Logout.
- Admin list.
- User toggle.
- Subject list.
- Document list.
- Chat session.
- Learning hub.
- Manual draft.
- Publish.
- Student visibility.
- Attempt.
- Submit.
- Analytics.
- Version.
- Restore.
- Audit.
- 429.
- Live.
- Ready.
- Error page.
- Mobile.
- Console.

## 33. Bug report template

### Tiêu đề

`[Module] Hành vi sai ngắn gọn`

### Môi trường

- Branch/commit.
- OS.
- Browser.
- Database.

### Tiền điều kiện

Role và data.

### Bước tái hiện

1. ...
2. ...
3. ...

### Kết quả thực tế

Mô tả.

### Kết quả mong đợi

Mô tả.

### Bằng chứng

- Screenshot.
- Trace ID.
- Log đã redact.

### Mức độ

- Blocker.
- Critical.
- Major.
- Minor.

## 34. Test completion report

Nêu:

- Scope.
- Build.
- Environment.
- Passed.
- Failed.
- Blocked.
- Known issue.
- Risk.
- Recommendation.

## 35. Exit criteria

Release có thể chấp nhận khi:

- Build pass.
- Critical suite pass.
- Không blocker.
- Security role pass.
- Data integrity pass.
- Health pass.
- Error page pass.
- Mobile journey pass.
- Không console error nghiêm trọng.
- Known issue được chấp nhận rõ.

## 36. Kết luận

Chất lượng của hệ thống này phụ thuộc nhiều vào kiểm thử lịch sử và quyền.

Không chỉ test “nút có bấm được”.

Phải test:

- Ai được bấm.
- Dữ liệu nào thay đổi.
- Dữ liệu cũ có còn.
- Có rollback không.
- Audit có ghi.
- Analytics có đúng.
- Dependency fail thì sao.

Playbook cần được cập nhật mỗi khi có bug production hoặc business rule mới.
