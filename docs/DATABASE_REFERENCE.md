# Từ điển dữ liệu và hướng dẫn bảo toàn dữ liệu

## 1. Mục tiêu

Tài liệu này mô tả mô hình dữ liệu của RAG Edu Hub ở mức phục vụ phát triển và vận hành.

Đây không phải bản dump schema.

Nội dung tập trung vào:

- Ý nghĩa nghiệp vụ.
- Quan hệ.
- Ownership.
- Delete behavior.
- Dữ liệu lịch sử.
- Query pattern.
- Rủi ro migration.
- Quy tắc bảo toàn.

Tên bảng và tên cột cần được đối chiếu với `RagEduContext`.

## 2. Database technology

Hệ thống sử dụng:

- SQL Server.
- Entity Framework Core.
- `RagEduContext`.
- Fluent configuration.
- Schema synchronization khi khởi động cho một số phần mở rộng.

Production nên có chiến lược migration rõ ràng.

Schema synchronization không thay thế hoàn toàn migration có version.

## 3. Quy ước chung

### 3.1. Khóa chính

Phần lớn bảng dùng khóa chính số nguyên `id`.

ID được sinh ở database.

Client không được tự đặt ID.

### 3.2. Khóa ngoại

Tên cột khóa ngoại thường có hậu tố `_id`.

Ví dụ:

- `user_id`.
- `subject_id`.
- `learning_set_id`.
- `question_bank_item_id`.

### 3.3. Timestamp

Các cột thường gặp:

- `created_at`.
- `updated_at`.
- `deleted_at`.
- `started_at`.
- `completed_at`.

Khuyến nghị lưu UTC.

Khi schema cũ dùng local time, phải ghi rõ trong migration.

### 3.4. Soft delete

Các bảng cần khôi phục thường có:

- `is_deleted`.
- `deleted_at`.
- `deleted_by`.

Query mặc định phải loại dữ liệu đã xóa nếu màn hình không phải thùng rác.

### 3.5. JSON columns

JSON được dùng cho dữ liệu cấu trúc linh hoạt:

- Danh sách lựa chọn.
- Nguồn tham chiếu.
- Snapshot phiên bản.
- Chi tiết audit.
- Payload callback.

JSON phải được validate trước khi lưu.

## 4. Sơ đồ miền dữ liệu

```text
users
  |-- subjects
  |-- documents
  |-- sessions
  |-- question_bank_items
  |-- learning_sets
  |-- learning_attempts
  |-- learning_set_versions
  |-- audit_logs
  |-- payment_tickets
  `-- user_subscriptions

subjects
  |-- chapters
  |-- documents
  |-- sessions
  |-- question_bank_items
  `-- learning_sets

documents
  `-- chunks
       `-- embeddings

learning_sets
  |-- learning_set_items
  |    `-- question_bank_items
  |-- learning_attempts
  |    `-- learning_attempt_answers
  `-- learning_set_versions
```

## 5. Nhóm identity

## 5.1. `users`

Mục đích:

Lưu tài khoản hệ thống.

Trường quan trọng:

| Cột | Ý nghĩa |
|---|---|
| `id` | Khóa chính |
| `username` | Tên đăng nhập |
| `email` | Email |
| `password` | Mật khẩu đã được hệ thống lưu theo cơ chế hiện có |
| `full_name` | Tên hiển thị |
| `role_id` | Vai trò |
| `subject_id` | Môn được phân công nếu là giảng viên |
| `is_active` | Trạng thái tài khoản |
| `last_login_at` | Lần đăng nhập gần nhất |
| `created_at` | Ngày tạo |
| `updated_at` | Ngày cập nhật |

Business rule:

- Username phải duy nhất.
- Email nên duy nhất.
- Tài khoản inactive không được đăng nhập.
- Sinh viên không cần `subject_id` để làm Quiz.
- Giảng viên cần `subject_id` cho nghiệp vụ quản lý tài liệu và môn.
- Không trả password qua DTO.

Rủi ro:

- Xóa user có thể phá nhiều quan hệ.
- Nên vô hiệu hóa thay vì hard delete.
- Audit log có thể cần giữ actor sau khi user nghỉ.

## 5.2. `roles`

Mục đích:

Lưu vai trò.

Các role nghiệp vụ:

- Quản trị viên.
- Giảng viên.
- Sinh viên.

Code hiện tại sử dụng role ID trong một số filter.

Nếu thay role ID, phải rà soát toàn bộ hardcoded rule.

Khuyến nghị dùng constant hoặc policy rõ ràng.

## 5.3. `permissions`

Mục đích:

Mô hình hóa quyền chi tiết.

Quan hệ role-permission qua bảng nối.

Không nên giả định có permission record là toàn bộ code đã dùng permission-based authorization.

Phải kiểm tra filter và helper hiện tại.

## 5.4. `login_logs`

Mục đích:

Theo dõi đăng nhập.

Trường thường dùng:

- `user_id`.
- `status`.
- `ip_address`.
- `user_agent`.
- `created_at`.

Không lưu password.

Không log token.

## 5.5. `refresh_tokens`

Mục đích:

Lưu refresh token nếu flow token được sử dụng.

Token nên lưu dạng hash.

Revocation phải được tôn trọng.

Không đưa token thô vào log.

## 6. Nhóm academic

## 6.1. `subjects`

Mục đích:

Lưu môn học.

Trường chính:

- `id`.
- `code`.
- `name`.
- `description`.
- `is_deleted`.
- `deleted_at`.
- `deleted_by`.
- `created_at`.

Business rule:

- Code nên duy nhất.
- Môn đã xóa mềm không xuất hiện trong danh sách mặc định.
- Không hard delete môn có dữ liệu học tập.

## 6.2. `chapters`

Mục đích:

Tổ chức tài liệu và câu hỏi theo chương.

Trường chính:

- `id`.
- `subject_id`.
- `number`.
- `title`.
- `description`.
- `created_at`.

Business rule:

- Chapter thuộc một subject.
- Chapter number nên ổn định trong subject.
- Xóa chapter cần xem xét documents và questions.

## 7. Nhóm tài liệu và RAG

## 7.1. `documents`

Mục đích:

Lưu metadata tài liệu.

Trường quan trọng:

- `id`.
- `subject_id`.
- `chapter_id`.
- `uploaded_by`.
- `filename`.
- `original_name`.
- `storage_path`.
- `file_type`.
- `file_size`.
- `file_hash`.
- `page_count`.
- `status`.
- `indexed_at`.
- `error_msg`.
- `is_deleted`.
- `deleted_at`.
- `deleted_by`.
- `created_at`.

Status thường biểu đạt:

- Đang xử lý.
- Đã index.
- Lỗi.

Business rule:

- Chỉ giảng viên đúng môn được upload.
- File type phải được allowlist.
- Hash giúp chống trùng nội dung.
- Storage path không nên lấy trực tiếp từ input.
- Xóa mềm không nhất thiết xóa file ngay.

## 7.2. `chunks`

Mục đích:

Lưu đoạn văn bản đã chia nhỏ.

Trường quan trọng:

- `document_id`.
- `chunking_config_id`.
- `chunk_index`.
- `content`.
- `page_number`.
- `char_start`.
- `char_end`.
- `token_count`.
- `metadata`.
- `created_at`.

Business rule:

- Chunk index duy trì thứ tự trong document.
- Content không được rỗng.
- Page number hỗ trợ citation.
- Char range hỗ trợ truy vết.

## 7.3. `chunking_configs`

Mục đích:

Lưu cấu hình chia đoạn.

Trường:

- `name`.
- `strategy`.
- `chunk_size`.
- `chunk_overlap`.
- `params`.
- `description`.

Lưu ý:

Không phải mọi strategy lưu trong DB đều chắc chắn có implementation.

Phải kiểm tra `DocumentService` và helper.

## 7.4. `embedding_models`

Mục đích:

Mô tả model embedding.

Trường:

- `provider`.
- `model_id`.
- `dimension`.
- `language`.
- `is_free`.
- `api_key_env`.
- `description`.

Không lưu API key trực tiếp.

`api_key_env` chỉ là tên nguồn cấu hình.

## 7.5. `embeddings`

Mục đích:

Lưu vector của chunk.

Trường:

- `chunk_id`.
- `embedding_model_id`.
- `vector`.
- `created_at`.

Business rule:

- Vector dimension phải khớp model.
- Re-index cần tránh để lại vector cũ không hợp lệ.
- Không trộn similarity giữa vector khác dimension.

## 8. Nhóm chat

## 8.1. `sessions`

Mục đích:

Lưu hội thoại.

Trường:

- `id`.
- `user_id`.
- `subject_id`.
- `title`.
- `is_archived`.
- `created_at`.
- `updated_at`.

Business rule:

- User chỉ xem session của mình.
- Session gắn với subject để giới hạn RAG.
- Archived session không bị hard delete mặc định.

## 8.2. `messages`

Mục đích:

Lưu message trong hội thoại.

Trường:

- `session_id`.
- `role`.
- `content`.
- `created_at`.

Role message thường gồm:

- User.
- Assistant.

Không lưu nội dung nhạy cảm ngoài nhu cầu.

## 8.3. `message_citations`

Mục đích:

Liên kết answer với chunk nguồn.

Trường:

- `message_id`.
- `chunk_id`.
- `rank_order`.
- `similarity_score`.
- `was_used`.

Citation giúp:

- Giải thích nguồn.
- Debug retrieval.
- Đánh giá RAG.

## 8.4. `student_chat_usages`

Mục đích:

Theo dõi quota chat.

Trường:

- `user_id`.
- `subject_id`.
- `window_start`.
- `question_count`.
- `created_at`.
- `updated_at`.

Unique rule nên ngăn trùng window cho cùng user-subject.

Update quota cần tránh race condition.

## 9. Nhóm ngân hàng câu hỏi

## 9.1. `question_bank_items`

Mục đích:

Lưu câu hỏi tái sử dụng.

Trường:

| Cột | Ý nghĩa |
|---|---|
| `id` | Khóa chính |
| `subject_id` | Môn học |
| `chapter_id` | Chương tùy chọn |
| `question_type` | Loại câu hỏi |
| `prompt` | Nội dung câu hỏi |
| `options_json` | Danh sách lựa chọn |
| `correct_answer` | Đáp án đúng |
| `explanation` | Giải thích |
| `difficulty` | Độ khó khai báo |
| `topic` | Chủ đề |
| `learning_objective` | Mục tiêu học tập |
| `source_references_json` | Nguồn |
| `created_by_user_id` | Người tạo |
| `is_ai_generated` | Có do AI hỗ trợ hay không |
| `ai_model` | Model kỹ thuật |
| `is_active` | Có thể tái sử dụng |
| `created_at` | Ngày tạo |
| `updated_at` | Ngày cập nhật |

Business rule cho multiple choice:

- `options_json` phải parse được.
- Danh sách phải có đúng bốn đáp án.
- Các đáp án nên khác nhau.
- `correct_answer` phải thuộc danh sách.
- Prompt không được rỗng.

Business rule cho true/false:

- Đáp án chuẩn hóa.
- UI hiển thị rõ.

Business rule cho short answer:

- Cần normalization khi chấm.
- Có thể cần danh sách đáp án chấp nhận.

Lịch sử:

Không nên thay đổi nghĩa câu hỏi đã xuất hiện trong attempt cũ.

Nếu chỉnh sửa lớn, tạo câu hỏi mới và deactivate câu cũ.

## 10. Nhóm bộ ôn tập và Quiz

## 10.1. `learning_sets`

Mục đích:

Lưu Quiz, flashcard set hoặc game set.

Trường:

- `id`.
- `subject_id`.
- `title`.
- `description`.
- `instructions`.
- `activity_type`.
- `duration_minutes`.
- `is_published`.
- `shuffle_questions`.
- `shuffle_options`.
- `created_by_user_id`.
- `ai_model`.
- `is_deleted`.
- `deleted_at`.
- `created_at`.
- `updated_at`.

Business rule:

- Chỉ giảng viên sở hữu được sửa.
- Bản nháp không xuất hiện cho sinh viên.
- Quiz đã phát hành xuất hiện cho mọi sinh viên.
- Xóa sử dụng soft delete.
- Restore từ thùng rác không tự phát hành.

## 10.2. `learning_set_items`

Mục đích:

Liên kết LearningSet và QuestionBankItem.

Trường:

- `learning_set_id`.
- `question_bank_item_id`.
- `order_index`.
- `points`.

Ràng buộc:

- Một câu không nên lặp trong cùng set.
- `order_index` nên duy nhất trong set.
- Points không âm.
- Shuffle không thay đổi dữ liệu order gốc.

## 11. Nhóm lượt làm

## 11.1. `learning_attempts`

Mục đích:

Lưu một lần sinh viên làm Quiz.

Trường:

- `id`.
- `learning_set_id`.
- `user_id`.
- `started_at`.
- `completed_at`.
- `score`.
- `total_points`.
- `correct_count`.
- `total_questions`.

Business rule:

- Attempt chỉ được tạo cho nội dung phù hợp.
- Sinh viên có thể làm Quiz của mọi môn nếu Quiz đã phát hành.
- Score do server tính.
- Completed time chỉ đặt khi nộp.
- Double submit cần được xử lý an toàn.

## 11.2. `learning_attempt_answers`

Mục đích:

Lưu câu trả lời ở grain một attempt-một question.

Trường:

- `learning_attempt_id`.
- `question_bank_item_id`.
- `selected_answer`.
- `is_correct`.
- `awarded_points`.
- `answered_at`.

Ràng buộc:

- Một question chỉ có một answer cuối trong attempt.
- `is_correct` do server tính.
- `awarded_points` không vượt points item.
- Answer phải giữ được để phân tích.

## 12. Nhóm phiên bản

## 12.1. `learning_set_versions`

Mục đích:

Lưu snapshot bất biến của Quiz.

Trường:

- `id`.
- `learning_set_id`.
- `version_number`.
- `snapshot_json`.
- `change_summary`.
- `created_by_user_id`.
- `created_at`.

Ràng buộc:

- Version number duy nhất trong một set.
- Snapshot không sửa sau khi tạo.
- Creator phải tồn tại khi tạo.
- Xóa LearningSet có thể cascade version nếu hard delete được phép.

Khuyến nghị:

- Snapshot có schema version.
- Parse phải chịu được field mới.
- Có checksum nếu compliance yêu cầu.

## 13. Nhóm audit

## 13.1. `audit_logs`

Mục đích:

Theo dõi thao tác ghi của staff.

Trường:

- `user_id`.
- `role_id`.
- `action`.
- `category`.
- `entity_type`.
- `entity_id`.
- `description`.
- `details_json`.
- `ip_address`.
- `user_agent`.
- `request_path`.
- `http_method`.
- `status_code`.
- `trace_identifier`.
- `created_at`.

Business rule:

- Lecturer chỉ xem log của mình.
- Admin xem toàn hệ thống.
- Audit record không được sửa bởi UI.
- Không ghi secret.
- Không ghi full request body tùy tiện.

Retention:

Chưa nên xóa log nếu chưa có policy.

Khi có policy, phải cân bằng:

- Điều tra.
- Pháp lý.
- Dung lượng.
- Quyền riêng tư.

## 14. Nhóm subscription

## 14.1. `subscription_plans`

Mục đích:

Lưu gói dịch vụ.

Trường:

- `name`.
- `description`.
- `price`.
- `duration_days`.
- `is_active`.
- `created_at`.

Không thay giá plan lịch sử nếu cần báo cáo chính xác.

Có thể cần version plan trong tương lai.

## 14.2. `payment_tickets`

Mục đích:

Lưu giao dịch.

Trường:

- `user_id`.
- `plan_id`.
- `amount`.
- `transfer_reference`.
- `payment_method`.
- `momo_order_id`.
- `momo_request_id`.
- `momo_trans_id`.
- `momo_pay_url`.
- `momo_response_json`.
- `momo_ipn_json`.
- `momo_result_code`.
- `status`.
- `admin_note`.
- `reviewed_by`.
- `reviewed_at`.
- `created_at`.

Business rule:

- Amount phải khớp plan tại thời điểm tạo.
- Callback phải verify signature.
- Complete phải idempotent.
- JSON callback có thể chứa dữ liệu nhạy cảm.

## 14.3. `user_subscriptions`

Mục đích:

Lưu quyền sử dụng.

Trường:

- `user_id`.
- `plan_id`.
- `start_at`.
- `end_at`.
- `is_active`.
- `payment_ticket_id`.
- `created_at`.

Business rule:

- Subscription mới có thể nối tiếp thời hạn cũ.
- Ticket không được cấp quyền hai lần.
- End time phải sau start time.

## 15. Index strategy

Index nên phục vụ:

- Username lookup.
- Email lookup.
- Subject filtering.
- Document status.
- Chunk by document.
- Embedding by chunk/model.
- Session by user.
- Message by session/time.
- Question by subject/active.
- Set by subject/published/deleted.
- Attempt by set.
- Attempt by user.
- Answer by attempt/question.
- Version by set/version number.
- Audit by user/time.
- Audit by category/action/time.
- Payment by user/status/time.

Không tạo index chỉ vì một cột tồn tại.

Mỗi index làm tăng chi phí ghi.

Query plan production nên được đo.

## 16. Delete behavior

### 16.1. Cascade phù hợp

Có thể cascade khi child không có ý nghĩa độc lập:

- Learning set item khi set hard delete.
- Version khi set hard delete theo policy.
- Attempt answer khi attempt hard delete theo retention policy.

### 16.2. Restrict phù hợp

Nên restrict khi cần bảo toàn lịch sử:

- User đang được tham chiếu.
- Question đã có answer.
- Subject có documents hoặc Quiz.
- Payment ticket đã cấp subscription.

### 16.3. Set null phù hợp

Audit actor có thể set null khi tài khoản bị xóa bắt buộc.

Nên giữ actor display snapshot trong audit nếu yêu cầu lâu dài.

## 17. Query patterns

### 17.1. Read-only

Dùng `AsNoTracking` khi:

- Chỉ hiển thị.
- Không update entity trong cùng context.
- Dashboard.
- Audit list.
- Question bank list.

### 17.2. Projection

Project trực tiếp sang DTO khi:

- Danh sách lớn.
- Chỉ cần ít cột.
- Có aggregate.
- Tránh Include graph.

### 17.3. Pagination

Pagination phải:

- Có order ổn định.
- Dùng `Skip` và `Take`.
- Trả total count.
- Giới hạn page size.
- Không load toàn bộ rồi mới phân trang.

### 17.4. Analytics

Analytics cần kiểm tra:

- Date range.
- Timezone.
- Attempt completed.
- Denominator.
- Zero rows.
- Null duration.
- Outlier.

## 18. Migration checklist

Trước migration:

- [ ] Backup database.
- [ ] Xác định downtime.
- [ ] Kiểm tra dung lượng.
- [ ] Kiểm tra dữ liệu null.
- [ ] Kiểm tra duplicate trước unique index.
- [ ] Viết backfill.
- [ ] Đo thời gian.
- [ ] Chuẩn bị rollback.
- [ ] Kiểm tra app version tương thích.

Trong migration:

- [ ] Dùng transaction nếu phù hợp.
- [ ] Không lock bảng lâu ngoài dự kiến.
- [ ] Ghi log tiến độ.
- [ ] Không in secret.
- [ ] Dừng nếu precondition sai.

Sau migration:

- [ ] Kiểm tra schema.
- [ ] Kiểm tra row count.
- [ ] Kiểm tra foreign key.
- [ ] Kiểm tra health ready.
- [ ] Chạy smoke test.
- [ ] Theo dõi error rate.

## 19. Backup và restore

Backup database phải đi cùng backup file tài liệu.

Nếu chỉ restore database nhưng thiếu storage file:

- Document record tồn tại.
- File vật lý không tồn tại.
- Re-index thất bại.

Nếu chỉ restore storage:

- File tồn tại.
- Database không có metadata.
- UI không truy cập được.

Recovery plan cần đồng bộ:

- SQL backup.
- Storage backup.
- Configuration version.
- Application build.

## 20. Data quality checks

Kiểm tra định kỳ:

- User trùng username.
- User role không tồn tại.
- Lecturer không có subject.
- Document status processing quá lâu.
- Chunk không có document.
- Embedding sai dimension.
- Session không có user.
- Message không có session.
- Citation không có chunk.
- Question active nhưng prompt rỗng.
- Multiple choice không đủ bốn option.
- Correct answer không thuộc option.
- Learning set item trùng question.
- Published set không có item.
- Attempt completed nhưng thiếu answer.
- Score lớn hơn total points.
- Version number trùng.
- Snapshot parse lỗi.
- Audit thiếu actor và mô tả.
- Payment approved nhưng thiếu subscription.

## 21. SQL investigation safety

Khi điều tra production:

- Ưu tiên SELECT.
- Luôn dùng WHERE.
- Kiểm tra row count trước DELETE.
- Dùng transaction cho sửa dữ liệu.
- Không chạy script chưa review.
- Không dán kết quả chứa thông tin cá nhân vào ticket công khai.
- Không đưa connection string vào terminal history nếu tránh được.

Mẫu quy trình sửa dữ liệu:

1. Chụp backup.
2. SELECT đúng record.
3. Ghi expected count.
4. BEGIN TRANSACTION.
5. Thực hiện update/delete.
6. SELECT kiểm chứng.
7. COMMIT khi đúng.
8. ROLLBACK khi sai.
9. Ghi incident note.

## 22. Definition of done cho thay đổi schema

- [ ] Entity cập nhật.
- [ ] Fluent configuration cập nhật.
- [ ] DbSet cập nhật nếu cần.
- [ ] Index được cân nhắc.
- [ ] Foreign key rõ delete behavior.
- [ ] Migration/synchronizer cập nhật.
- [ ] Existing data có backfill.
- [ ] Query tương thích.
- [ ] Test dữ liệu cũ.
- [ ] Test dữ liệu mới.
- [ ] Documentation cập nhật.
- [ ] Rollback có kế hoạch.

## 23. Kết luận

Mô hình dữ liệu không chỉ phục vụ CRUD.

Nó phải bảo toàn lịch sử học tập, quyền truy cập, khả năng khôi phục và truy vết.

Đặc biệt với Quiz:

- Question là tài sản tái sử dụng.
- Set là cấu trúc tổ chức.
- Item là quan hệ và điểm.
- Attempt là sự kiện học tập.
- Answer là bằng chứng.
- Version là khả năng quay lại.
- Audit là trách nhiệm giải trình.

Không tối ưu dung lượng bằng cách xóa lịch sử nếu chưa có retention policy.
