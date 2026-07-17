# Đặc tả AI, ngân hàng câu hỏi và hệ thống Quiz

## 1. Mục tiêu

Tài liệu này mô tả toàn bộ vòng đời nội dung ôn tập.

Phạm vi bắt đầu từ tài liệu môn học.

Phạm vi kết thúc ở dashboard giảng viên và việc khôi phục phiên bản.

Các chức năng chính:

- Tạo câu hỏi bằng AI.
- Tạo câu hỏi thủ công.
- Lưu câu hỏi vào ngân hàng.
- Tổng hợp Quiz.
- Xáo trộn câu hỏi và đáp án.
- Flashcard.
- Trò chơi.
- Autosave.
- Phát hành.
- Làm bài.
- Chấm điểm.
- Analytics.
- Version history.
- Restore.

## 2. Nguyên tắc sản phẩm

### 2.1. AI là trợ lý

AI hỗ trợ giảng viên.

AI không thay thế quyền quyết định học thuật.

Nội dung do AI tạo phải ở trạng thái có thể review.

Giảng viên có thể:

- Chỉnh prompt.
- Chỉnh đáp án.
- Chỉnh đáp án đúng.
- Chỉnh giải thích.
- Chỉnh độ khó.
- Xóa câu không phù hợp.
- Thêm câu thủ công.
- Phát hành khi hài lòng.

### 2.2. Manual-first vẫn được hỗ trợ

Giảng viên không bắt buộc dùng AI.

Trình soạn thủ công phải đầy đủ:

- Tạo.
- Xem.
- Sửa.
- Xóa mềm.
- Khôi phục.
- Autosave.
- Versioning.
- Publish.
- Unpublish.

### 2.3. Sinh viên không cần phân môn

Quy tắc:

Mọi sinh viên đã đăng nhập đều có thể làm mọi Quiz đã phát hành.

Subject assignment của sinh viên không phải điều kiện.

Subject vẫn dùng để:

- Phân loại.
- Hiển thị.
- Thống kê.
- Quản lý ownership của giảng viên.

### 2.4. Lịch sử quan trọng hơn chỉnh sửa tại chỗ

Không được làm mất bằng chứng của attempt cũ.

Không được thay nghĩa answer cũ khi sửa question.

Version và soft delete được ưu tiên.

## 3. Khái niệm

### 3.1. Question bank item

Một câu hỏi độc lập có thể tái sử dụng.

Nó chứa:

- Prompt.
- Type.
- Options.
- Correct answer.
- Explanation.
- Difficulty.
- Topic.
- Learning objective.
- Source references.
- Creator.
- AI provenance.

### 3.2. Learning set

Một tập hoạt động ôn tập.

Ví dụ:

- Quiz.
- Flashcard set.
- Matching game.
- Timed challenge.

### 3.3. Learning set item

Quan hệ giữa set và question.

Nó giữ:

- Thứ tự.
- Điểm.

### 3.4. Attempt

Một phiên làm bài.

Attempt thuộc:

- Một student.
- Một learning set.

### 3.5. Answer

Câu trả lời của student cho một question trong attempt.

### 3.6. Version

Snapshot bất biến của learning set tại một thời điểm.

## 4. Vai trò và quyền

| Hành động | Admin | Lecturer | Student |
|---|---:|---:|---:|
| Xem Quiz đã phát hành | Có thể | Có thể | Có |
| Làm Quiz đã phát hành | Theo nhu cầu | Theo nhu cầu | Có |
| Xem ngân hàng câu hỏi môn phụ trách | Không mặc định | Có | Không |
| Tạo câu thủ công | Không mặc định | Có | Không |
| Tạo câu bằng AI | Không mặc định | Có | Không |
| Sửa câu hỏi | Không mặc định | Có theo ownership | Không |
| Tạo Quiz | Không mặc định | Có | Không |
| Phát hành | Không mặc định | Có theo ownership | Không |
| Xem dashboard | Không mặc định | Có theo môn | Không |
| Xem version | Không mặc định | Có theo ownership | Không |
| Restore version | Không mặc định | Có theo ownership | Không |
| Xem audit toàn hệ thống | Có | Không | Không |
| Xem audit của mình | Có | Có | Không |

Admin access có thể mở rộng sau.

Không suy ra admin được mọi quyền nếu code chưa cho phép.

## 5. Nguồn dữ liệu cho AI

AI chỉ nên nhận nội dung thuộc phạm vi giảng viên được phép.

Nguồn có thể gồm:

- Document của subject.
- Chunk đã index.
- Chapter cụ thể.
- Chủ đề do giảng viên nhập.
- Learning objective.
- Câu hỏi hiện có để tránh trùng.

Không gửi:

- Tài liệu môn khác.
- Dữ liệu sinh viên.
- Password.
- API key.
- Payment data.
- Audit log.

## 6. Quy trình AI generation

### 6.1. Input

Input có thể gồm:

- Subject ID.
- Document IDs.
- Chapter ID.
- Số lượng câu.
- Loại câu.
- Mức độ khó.
- Ngôn ngữ.
- Chủ đề.
- Mục tiêu học tập.
- Yêu cầu bổ sung.

### 6.2. Authorization

Trước khi gọi AI:

1. Kiểm tra user đăng nhập.
2. Kiểm tra role giảng viên.
3. Kiểm tra subject assignment.
4. Kiểm tra document thuộc subject.
5. Kiểm tra document có thể dùng.
6. Kiểm tra quota/rate limit.

### 6.3. Context assembly

Context phải:

- Đủ liên quan.
- Không quá dài.
- Có nguồn.
- Không trộn môn.
- Không chứa HTML rác.
- Không chứa prompt injection được tin cậy.

Document text là dữ liệu không đáng tin.

Prompt hệ thống phải nói rõ không làm theo chỉ dẫn nằm trong tài liệu.

### 6.4. Prompt contract

Prompt nên yêu cầu:

- Chỉ dựa trên nội dung nguồn.
- Không bịa.
- Tiếng Việt đầy đủ dấu.
- Không dùng emoji.
- Output JSON.
- Đúng schema.
- Đúng số câu.
- Đúng bốn option cho multiple choice.
- Có correct answer.
- Có explanation.
- Có difficulty.
- Có source reference.

### 6.5. Provider call

Provider hiện được cấu hình qua backend.

UI chỉ hiển thị “AI”.

Client phải:

- Dùng timeout.
- Hủy request theo CancellationToken.
- Không log API key.
- Phân loại lỗi.
- Kiểm tra response rỗng.
- Giới hạn retry.

### 6.6. Parse

Response phải parse thành model cấu trúc.

Nếu có markdown fence:

- Loại fence an toàn.
- Parse JSON.

Nếu JSON lỗi:

- Không lưu một phần không kiểm soát.
- Trả thông báo rõ.
- Cho phép thử lại.

### 6.7. Validate

Mỗi question phải qua validator.

Validator multiple choice:

- Prompt có nội dung.
- Có chính xác bốn option.
- Option không rỗng.
- Option không trùng sau trim.
- Correct answer khớp một option.
- Explanation có thể rỗng nhưng nên có.
- Difficulty thuộc allowlist.

Validator true/false:

- Correct answer là giá trị hợp lệ.
- Không có option tùy ý.

Validator short answer:

- Có đáp án mẫu.
- Có normalization rule.

### 6.8. Save draft

AI output lưu dưới dạng draft.

Question ghi `is_ai_generated`.

Model name chỉ phục vụ kỹ thuật.

Không hiển thị tên provider ở CTA.

### 6.9. Review

Giảng viên review:

- Độ chính xác.
- Tính rõ ràng.
- Một đáp án đúng duy nhất.
- Distractor hợp lý.
- Không lộ đáp án trong prompt.
- Không phụ thuộc thông tin ngoài tài liệu.
- Phù hợp độ khó.
- Phù hợp mục tiêu học tập.

## 7. Cấu trúc câu multiple choice

### 7.1. Prompt tốt

Prompt tốt:

- Có một vấn đề rõ.
- Không mơ hồ.
- Không phủ định kép.
- Không chứa dữ kiện thừa.
- Không dùng “tất cả đáp án trên” nếu không cần.
- Không gợi ý đáp án bằng độ dài.

### 7.2. Bốn đáp án

Mỗi câu trắc nghiệm phải có:

- Một đáp án đúng.
- Ba distractor.

Tất cả option:

- Cùng kiểu ngữ pháp.
- Độ dài tương đối cân bằng.
- Không trùng nghĩa hoàn toàn.
- Không có option rỗng.
- Không dùng ký hiệu gây lộ.

### 7.3. Correct answer

Correct answer lưu theo nội dung hoặc key theo schema hiện tại.

Khi shuffle:

- Correctness không phụ thuộc index cũ.
- Mapping phải được giữ.

### 7.4. Explanation

Explanation nên:

- Giải thích vì sao đáp án đúng.
- Nêu vì sao distractor sai khi phù hợp.
- Dẫn nguồn.
- Ngắn gọn.
- Không thêm kiến thức không có nguồn.

## 8. Tạo câu thủ công

### 8.1. Create

Giảng viên chọn “Tạo Quiz thủ công”.

Trình soạn khởi tạo draft.

Draft có:

- Title mặc định.
- Subject từ session.
- Activity type.
- Question list rỗng.
- Autosave state.

### 8.2. Add question

Giảng viên có thể:

- Chọn từ ngân hàng.
- Tạo câu mới.
- Nhân bản câu.
- Sắp xếp.
- Đặt điểm.

### 8.3. Edit question

Khi edit:

- Validation chạy client để phản hồi nhanh.
- Validation server vẫn bắt buộc.
- Autosave không publish.
- Trạng thái lưu được hiển thị.

### 8.4. Delete question

Trong editor:

- Xóa khỏi set không nhất thiết xóa khỏi bank.
- UI phải nói rõ phạm vi.
- Có confirm khi nguy hiểm.
- Có khả năng undo qua draft/version.

### 8.5. Reorder

Reorder cập nhật `order_index`.

Server phải normalize order:

- 0..n-1 hoặc 1..n.
- Không trùng.
- Không thiếu.

## 9. Autosave

### 9.1. Mục tiêu

Autosave bảo vệ khi:

- Mất điện.
- Mất mạng.
- Browser crash.
- Tab đóng nhầm.
- Session gián đoạn.

### 9.2. Local draft

Client có thể lưu local storage.

Không lưu:

- Secret.
- Dữ liệu người khác.
- File lớn.

Key nên gồm:

- User ID dạng không nhạy cảm hoặc scope.
- Learning set ID.
- Editor version.

### 9.3. Server autosave

Autosave server:

- Có debounce.
- Không gửi mỗi keystroke.
- Gửi revision.
- Trả saved time.
- Không tạo version history quá dày nếu policy loại autosave.

### 9.4. Conflict

Khi server mới hơn local:

- Không overwrite im lặng.
- Hiển thị lựa chọn.
- Cho xem thời gian.
- Có thể tải cả hai.

### 9.5. Offline

Khi offline:

- Hiển thị “Đang lưu trên thiết bị”.
- Queue thay đổi.
- Khi online, đồng bộ.
- Nếu conflict, dừng và hỏi.

Không hiển thị “Đã lưu” nếu chỉ mới ở memory.

## 10. Question bank

### 10.1. List

Danh sách cần:

- Search.
- Filter subject.
- Filter chapter.
- Filter type.
- Filter difficulty.
- Filter source.
- Filter AI/manual.
- Filter active.
- Pagination.

### 10.2. Reuse

Giảng viên chọn câu từ bank.

Khi thêm vào Quiz:

- Không duplicate trong cùng Quiz.
- Giữ question ID.
- Đặt order.
- Đặt points.

### 10.3. Random compose

Compose từ bank có thể:

- Lọc theo topic.
- Lọc theo difficulty.
- Chọn số lượng.
- Tránh câu đã dùng gần đây.
- Cân bằng coverage.
- Shuffle.

### 10.4. Deactivate

Deactivate câu:

- Không xóa answer cũ.
- Không nhất thiết gỡ khỏi Quiz đã có.
- Không được chọn cho Quiz mới mặc định.

### 10.5. Duplicate detection

Có thể kiểm tra:

- Prompt normalized trùng.
- Similarity cao.
- Cùng source.
- Cùng correct answer và options.

AI similarity chỉ hỗ trợ.

Giảng viên quyết định.

## 11. Compose Quiz

### 11.1. Metadata

Quiz metadata:

- Title.
- Description.
- Instructions.
- Subject.
- Duration.
- Shuffle question.
- Shuffle option.
- Publish state.

### 11.2. Selection

Question selection:

- Manual pick.
- AI generate.
- Random bank.
- Mix.

### 11.3. Quality gate

Trước publish:

- Title không rỗng.
- Có ít nhất một question.
- Mọi multiple choice có bốn option.
- Mọi question có correct answer.
- Points hợp lệ.
- Duration hợp lệ.
- Không có inactive/broken question.
- Không có duplicate.

### 11.4. Publish

Publish:

- Xác thực owner.
- Chạy quality gate.
- Tạo version.
- Đặt `is_published`.
- Ghi audit.
- Trả success message.

### 11.5. Unpublish

Unpublish:

- Không xóa attempt.
- Ngăn attempt mới nếu policy.
- Giữ version.
- Ghi audit.

## 12. Sinh viên khám phá Quiz

### 12.1. Visibility query

Danh sách sinh viên lọc:

- `is_published = true`.
- `is_deleted = false`.

Không lọc theo `student.subject_id`.

### 12.2. Subject display

Mặc dù không phân quyền theo môn, UI vẫn hiển thị:

- Mã môn.
- Tên môn.
- Quiz title.
- Số câu.
- Duration.

### 12.3. Empty state

Nếu chưa có Quiz:

- Nói rõ chưa có nội dung.
- Không báo lỗi.
- Cho quay lại trang chủ.

## 13. Bắt đầu attempt

### 13.1. Preconditions

- User đăng nhập.
- Role phù hợp.
- Quiz tồn tại.
- Quiz published.
- Quiz không deleted.

### 13.2. Snapshot câu hỏi

Rủi ro:

Quiz có thể được sửa khi student đang làm.

Chiến lược hiện tại cần bảo toàn question IDs.

Khuyến nghị mạnh:

- Attempt snapshot.
- Hoặc version ID trên attempt.

Điều này giúp chấm đúng phiên bản student đã thấy.

### 13.3. Shuffle questions

Shuffle:

- Thực hiện một lần khi bắt đầu.
- Giữ order trong attempt/session.
- Không shuffle lại mỗi refresh.

### 13.4. Shuffle options

Shuffle option:

- Giữ mapping đáp án đúng.
- Giữ order ổn định trong attempt.
- Không dựa vào label A/B/C/D cố định.

## 14. Làm bài

### 14.1. Navigation

UI nên hỗ trợ:

- Câu trước.
- Câu sau.
- Danh sách số câu.
- Trạng thái đã trả lời.
- Thời gian còn lại.
- Nộp bài.

### 14.2. Answer save

Có thể lưu answer tạm.

Server không tin answer client sau khi completed.

### 14.3. Timer

Client timer phục vụ UX.

Server time là nguồn quyết định.

Không tin duration do client gửi.

### 14.4. Refresh

Refresh không được:

- Tạo attempt mới.
- Mất answer.
- Shuffle lại.
- Reset timer.

## 15. Nộp bài và chấm

### 15.1. Rate limit

Submit được rate limit để chống:

- Double click.
- Script flood.
- Tạo attempt rác.

### 15.2. Idempotency

Nếu attempt đã completed:

- Không chấm lại thành record mới.
- Trả kết quả hiện có hoặc conflict phù hợp.

### 15.3. Server scoring

Server:

1. Load attempt.
2. Load question/items.
3. Normalize answer.
4. So sánh correct answer.
5. Tính awarded points.
6. Tính correct count.
7. Tính score.
8. Set completed time.
9. Lưu transaction.

### 15.4. Multiple choice normalization

Normalization:

- Trim.
- So sánh theo rule xác định.
- Không đổi dấu nếu option phân biệt.
- Không tin index nếu options shuffled.

### 15.5. Short answer normalization

Cần định nghĩa:

- Case sensitivity.
- Trim.
- Unicode normalization.
- Dấu câu.
- Khoảng trắng.
- Synonym.

Nếu chưa có rubric mạnh, không nên chấm fuzzy quá rộng.

## 16. Kết quả sinh viên

Kết quả có thể hiển thị:

- Score.
- Correct count.
- Total questions.
- Duration.
- Từng câu.
- Answer đã chọn.
- Correct answer.
- Explanation.

Nếu Quiz dùng cho đánh giá chính thức:

Có thể ẩn correct answer đến thời điểm phù hợp.

Đây là policy sản phẩm.

## 17. Dashboard giảng viên

### 17.1. Filter

Filter:

- Subject.
- Quiz.
- 7 ngày.
- 30 ngày.
- 90 ngày.

### 17.2. Tổng lượt làm

Công thức:

```text
count(attempt đã hoàn thành trong phạm vi)
```

### 17.3. Sinh viên tham gia

Công thức:

```text
distinct count(user_id)
```

### 17.4. Điểm trung bình

Công thức:

```text
average(score / total_points * 100)
```

Phải xử lý `total_points = 0`.

### 17.5. Tỷ lệ đạt

Ngưỡng hiện tại:

```text
percentage >= 50
```

Công thức:

```text
passed attempts / total attempts * 100
```

### 17.6. Thời gian trung bình

Công thức:

```text
average(completed_at - started_at)
```

Loại duration:

- Âm.
- Null.
- Bất thường trên 24 giờ theo rule hiện tại.

### 17.7. Trend

Trend tạo bucket theo ngày.

Ngày không có attempt vẫn hiển thị zero.

Điều này tránh biểu đồ gây hiểu nhầm.

### 17.8. Per-Quiz

Mỗi row:

- Quiz.
- Attempts.
- Unique students.
- Average score.
- Pass rate.
- Last attempt.

## 18. Question analytics

### 18.1. Tỷ lệ đúng

Công thức:

```text
correct answers / total answers * 100
```

Denominator là số answer thực tế cho câu.

### 18.2. Độ khó thực nghiệm

| Tỷ lệ đúng | Nhãn |
|---:|---|
| Từ 80% | Dễ |
| Từ 50% đến dưới 80% | Trung bình |
| Dưới 50% | Khó |

Độ khó thực nghiệm khác độ khó do giảng viên khai báo.

Cả hai có thể cùng tồn tại.

### 18.3. Option distribution

Mỗi configured option hiển thị:

- Label.
- Selection count.
- Selection percentage.
- Có phải correct option.

Option 0 selection vẫn hiển thị.

### 18.4. Diễn giải

Tình huống cần chú ý:

- Correct rate rất thấp.
- Một distractor được chọn quá nhiều.
- Không ai chọn một distractor.
- Correct rate 100% với sample lớn.
- Sample quá nhỏ.

Không kết luận câu hỏng chỉ từ một metric.

Giảng viên cần xem:

- Wording.
- Source.
- Teaching coverage.
- Sample size.
- Student cohort.

## 19. Version history

### 19.1. Khi tạo version

Version có thể tạo khi:

- AI compose hoàn tất.
- Manual save quan trọng.
- Publish.
- Unpublish.
- Restore.

Autosave nhỏ không nhất thiết tạo version.

### 19.2. Version metadata

Hiển thị:

- `v1`, `v2`, ...
- Created time.
- Creator.
- Change summary.
- Publish state snapshot.
- Question count.
- Duration.

### 19.3. Initial backfill

Quiz cũ chưa có version:

- Khi mở history, hệ thống có thể tạo initial snapshot.
- Summary ghi rõ khởi tạo lịch sử.

### 19.4. Restore

Restore:

- Tự backup current.
- Apply selected snapshot.
- Set draft.
- Tạo version mới.
- Không xóa version cũ.
- Không xóa attempts.

### 19.5. Restore confirmation

Dialog phải nói:

- Version nào.
- Thao tác sẽ tạo backup.
- Quiz trở thành draft.
- Attempt cũ không bị ảnh hưởng.

## 20. Recycle bin

### 20.1. Soft delete Quiz

Soft delete:

- Set `is_deleted`.
- Set deleted time.
- Ẩn khỏi student.
- Giữ attempts.
- Giữ versions.

### 20.2. Restore Quiz

Restore:

- Clear deleted flag.
- Trở về draft nếu cần.
- Không tự publish.
- Ghi audit.

### 20.3. Permanent delete

Permanent delete chỉ nên có:

- Quyền cao.
- Confirm mạnh.
- Dependency check.
- Retention policy.
- Backup.
- Audit.

## 21. Audit events

Các event mong đợi:

- Tạo câu hỏi.
- Cập nhật câu hỏi.
- Xóa câu hỏi.
- Tạo Quiz.
- Cập nhật Quiz.
- Publish.
- Unpublish.
- Soft delete.
- Restore recycle.
- Restore version.
- AI generation.

Không audit autosave mỗi vài giây nếu gây noise.

Có thể aggregate autosave.

## 22. Failure modes

### 22.1. AI timeout

Hành vi:

- Không mất manual draft.
- Không tạo record rỗng.
- Thông báo tiếng Việt.
- Cho thử lại.

### 22.2. AI malformed JSON

Hành vi:

- Parse fail an toàn.
- Không lưu một phần không rõ.
- Log trace.
- Không hiển thị raw provider payload.

### 22.3. Mất mạng khi edit

Hành vi:

- Lưu local.
- Hiển thị offline.
- Đồng bộ lại.

### 22.4. Database lỗi khi save

Hành vi:

- Không báo “Đã lưu”.
- Giữ local draft.
- Cho retry.
- Trace identifier.

### 22.5. Restore snapshot lỗi

Hành vi:

- Transaction rollback.
- Current set không đổi.
- Log lỗi.
- Version record không dở dang.

### 22.6. Submit hai lần

Hành vi:

- Chỉ một completion.
- Kết quả nhất quán.
- Không duplicate answers.

## 23. Accessibility

Editor:

- Label cho input.
- Keyboard reorder alternative.
- Error gắn với field.
- Focus vào lỗi đầu.
- Không chỉ dùng màu.

Quiz player:

- Radio có label.
- Timer được đọc hợp lý.
- Button đủ kích thước.
- Contrast đủ.
- Không animation bắt buộc.

Analytics:

- Chart có accessible name.
- Metric có text.
- Table có header.
- Percentage không chỉ biểu diễn bằng bar.

## 24. Performance

### 24.1. Question bank

- Pagination.
- Projection.
- Index filter.
- Debounce search.

### 24.2. Dashboard

- Filter date ở SQL.
- Tránh load toàn bộ lịch sử.
- Aggregate ở database khi scale lớn.
- Cache ngắn nếu phù hợp.

### 24.3. Version snapshot

- Không load snapshot khi chỉ cần list metadata nếu scale lớn.
- Có thể tách preview.
- Giới hạn kích thước set.

### 24.4. AI

- Rate limit.
- Timeout.
- Context cap.
- Không gửi cùng tài liệu lặp.
- Theo dõi latency.

## 25. Acceptance criteria tổng

### 25.1. AI generation

- [ ] Lecturer đúng môn có thể mở trang.
- [ ] Student bị chặn.
- [ ] Input invalid không gọi provider.
- [ ] Multiple choice có bốn option.
- [ ] Correct answer hợp lệ.
- [ ] Draft được lưu.
- [ ] UI dùng chữ AI.
- [ ] Request thứ vượt quota nhận 429.

### 25.2. Manual editor

- [ ] Tạo được Quiz không dùng AI.
- [ ] Thêm câu được.
- [ ] Sửa câu được.
- [ ] Xóa khỏi set được.
- [ ] Reorder được.
- [ ] Autosave hoạt động.
- [ ] Offline draft không mất.
- [ ] Validation rõ.

### 25.3. Student

- [ ] Student không có subject vẫn thấy Quiz published.
- [ ] Student thấy Quiz môn khác.
- [ ] Draft không thấy.
- [ ] Deleted không thấy.
- [ ] Làm bài được.
- [ ] Refresh không tạo attempt mới.
- [ ] Submit được chấm server.

### 25.4. Analytics

- [ ] Total attempts đúng.
- [ ] Unique students đúng.
- [ ] Average đúng.
- [ ] Pass rate đúng.
- [ ] Duration loại outlier.
- [ ] Zero-day hiển thị.
- [ ] Option 0% hiển thị.
- [ ] Difficulty đúng ngưỡng.

### 25.5. Version

- [ ] Initial version được tạo.
- [ ] Publish tạo version.
- [ ] Restore backup current.
- [ ] Restore set draft.
- [ ] Version number tăng.
- [ ] Attempts không mất.
- [ ] Audit được ghi.

## 26. Roadmap khuyến nghị

Các cải tiến có giá trị:

- Attempt gắn version ID.
- Optimistic concurrency cho editor.
- Rubric cho short answer.
- Item analysis nâng cao.
- Discrimination index.
- Cronbach alpha cho bài đủ sample.
- Export analytics.
- Question exposure control.
- Blueprint theo learning objective.
- Scheduled publish.
- Cohort comparison.
- AI evaluation pipeline.
- Provider abstraction hoàn chỉnh.

Các mục trên là khuyến nghị.

Không được mô tả là đã triển khai.

## 27. Kết luận

Hệ thống Quiz đáng tin cậy cần nhiều hơn một form tạo câu hỏi.

Nó cần:

- Nguồn tài liệu.
- Validation.
- Human review.
- Ngân hàng tái sử dụng.
- Autosave.
- Versioning.
- Server scoring.
- Analytics đúng grain.
- Audit.
- Authorization.

AI chỉ là một bước trong vòng đời đó.

Giảng viên luôn giữ quyền kiểm soát nội dung trước khi phát hành.
