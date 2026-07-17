# Trung tâm tài liệu kỹ thuật RAG Edu Hub

## 1. Mục đích

Thư mục `docs` là nguồn tham chiếu kỹ thuật chính thức cho dự án RAG Edu Hub.

Tài liệu được viết cho sinh viên, giảng viên, lập trình viên, người kiểm thử và người vận hành hệ thống.

Mục tiêu của bộ tài liệu là giúp một thành viên mới có thể:

- Hiểu bài toán mà hệ thống đang giải quyết.
- Hiểu kiến trúc ba lớp của solution .NET.
- Xác định đúng nơi cần sửa khi có yêu cầu mới.
- Không đưa business logic vào Razor Page.
- Không truy cập trực tiếp database từ Presentation.
- Không làm mất lịch sử Quiz và kết quả làm bài.
- Không vô tình mở rộng quyền truy cập.
- Biết cách cấu hình AI mà không đưa API key lên Git.
- Biết cách kiểm tra health check và rate limiting.
- Biết cách điều tra lỗi bằng trace identifier và audit log.
- Biết cách triển khai thay đổi an toàn.

## 2. Phạm vi

Bộ tài liệu bao phủ ba project chính:

| Project | Vai trò |
|---|---|
| `Assignment1_Repository` | Entity, DbContext, repository và truy cập dữ liệu |
| `Assignment1_Service` | Business rule, DTO, AI, thống kê và orchestration |
| `Assignmet1_Presentation` | Razor Pages, middleware, filter, giao diện và HTTP pipeline |

Các miền nghiệp vụ được mô tả:

- Tài khoản và phân quyền.
- Môn học.
- Quản lý tài liệu.
- Trích xuất và chia nhỏ nội dung.
- Embedding và truy xuất RAG.
- Chat AI.
- Ngân hàng câu hỏi.
- Quiz thủ công.
- Quiz do AI hỗ trợ.
- Flashcard.
- Trò chơi ôn tập.
- Làm bài và chấm điểm.
- Dashboard thống kê.
- Phân tích độ khó câu hỏi.
- Lịch sử phiên bản Quiz.
- Nhật ký thao tác.
- Gói học và thanh toán MoMo.
- Health check.
- Rate limiting.
- Xử lý lỗi tập trung.

## 3. Bản đồ tài liệu

### 3.1. Kiến trúc

Đọc [ARCHITECTURE_HANDBOOK.md](ARCHITECTURE_HANDBOOK.md) khi cần:

- Hiểu ranh giới giữa ba layer.
- Hiểu dependency direction.
- Theo dõi một HTTP request từ trình duyệt đến SQL Server.
- Xác định vị trí đặt entity, DTO, service hoặc page handler.
- Đánh giá một thay đổi kiến trúc.
- Chuẩn bị thuyết trình về solution.

### 3.2. Cơ sở dữ liệu

Đọc [DATABASE_REFERENCE.md](DATABASE_REFERENCE.md) khi cần:

- Hiểu nhóm bảng chính.
- Hiểu quan hệ của Quiz, câu hỏi, lượt làm và phiên bản.
- Xác định delete behavior.
- Viết truy vấn điều tra dữ liệu.
- Chuẩn bị migration hoặc schema synchronization.
- Kiểm tra tính toàn vẹn dữ liệu.

### 3.3. AI và hệ thống ôn tập

Đọc [AI_QUIZ_AND_LEARNING_GUIDE.md](AI_QUIZ_AND_LEARNING_GUIDE.md) khi cần:

- Hiểu cách tài liệu trở thành câu hỏi.
- Hiểu ranh giới trách nhiệm giữa AI và giảng viên.
- Hiểu cấu trúc câu hỏi trắc nghiệm bốn đáp án.
- Hiểu cách tái sử dụng ngân hàng câu hỏi.
- Hiểu autosave, backup và version restore.
- Hiểu cách chấm điểm và thống kê.

### 3.4. Bảo mật và API

Đọc [SECURITY_AND_HTTP_GUIDE.md](SECURITY_AND_HTTP_GUIDE.md) khi cần:

- Kiểm tra đăng nhập và role.
- Hiểu page filter.
- Hiểu CSRF và form POST.
- Hiểu rate limiting.
- Hiểu CSP và security headers.
- Xử lý bí mật cấu hình.
- Phân loại lỗi HTTP.

### 3.5. Vận hành

Đọc [OPERATIONS_RUNBOOK.md](OPERATIONS_RUNBOOK.md) khi cần:

- Khởi động ứng dụng.
- Kiểm tra SQL Server.
- Dùng health endpoint.
- Điều tra HTTP 429 hoặc HTTP 500.
- Xử lý lỗi AI.
- Backup và phục hồi.
- Chuẩn bị triển khai.
- Ứng phó sự cố.

### 3.6. Kiểm thử

Đọc [TESTING_AND_QA_PLAYBOOK.md](TESTING_AND_QA_PLAYBOOK.md) khi cần:

- Lập kế hoạch kiểm thử.
- Kiểm thử quyền truy cập.
- Kiểm thử CRUD Quiz.
- Kiểm thử autosave và restore.
- Kiểm thử dashboard.
- Kiểm thử tỷ lệ đáp án.
- Kiểm thử responsive.
- Viết test tự động trong tương lai.

### 3.7. UI/UX

Đọc [UI_UX_DESIGN_SYSTEM.md](UI_UX_DESIGN_SYSTEM.md) khi cần:

- Giữ giao diện nhất quán.
- Dùng token màu và khoảng cách.
- Thiết kế trạng thái loading, empty và error.
- Thiết kế form dài.
- Thiết kế bảng responsive.
- Kiểm tra accessibility.
- Tránh hiệu ứng gây nhiễu.

### 3.8. Hướng dẫn phát triển

Đọc [DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md) khi cần:

- Thiết lập môi trường mới.
- Chạy solution.
- Thêm tính năng.
- Review code.
- Đặt tên.
- Ghi log.
- Chuẩn bị pull request.

### 3.9. Workflow cũ

[WORKFLOW_ANALYSIS.md](WORKFLOW_ANALYSIS.md) ghi lại phân tích trước đây về:

- Upload và indexing tài liệu.
- Chat RAG.
- Embedding.
- Thanh toán.
- Subscription.

Tài liệu này hữu ích như một bản ghi khảo sát.

Khi thông tin khác với handbook mới, cần kiểm tra source code hiện tại trước khi kết luận.

## 4. Thứ tự đọc đề xuất

### 4.1. Thành viên backend mới

1. Đọc tài liệu này.
2. Đọc kiến trúc tổng thể.
3. Đọc cơ sở dữ liệu.
4. Đọc AI và Quiz.
5. Đọc bảo mật.
6. Đọc runbook.
7. Đọc playbook kiểm thử.

### 4.2. Thành viên frontend mới

1. Đọc tài liệu này.
2. Đọc phần Presentation trong handbook kiến trúc.
3. Đọc UI/UX design system.
4. Đọc các test case responsive.
5. Chạy ứng dụng và duyệt toàn bộ Razor Pages.

### 4.3. Người kiểm thử

1. Đọc vai trò người dùng.
2. Đọc vòng đời Quiz.
3. Đọc testing playbook.
4. Đọc phần incident và audit log.
5. Chuẩn bị test data riêng biệt.

### 4.4. Người vận hành

1. Đọc cấu hình môi trường.
2. Đọc health check.
3. Đọc rate limiting.
4. Đọc error handling.
5. Đọc backup và recovery.
6. Đọc incident response.

### 4.5. Người chấm hoặc giảng viên hướng dẫn

1. Đọc executive summary trong handbook kiến trúc.
2. Đọc use case AI và Quiz.
3. Đọc dashboard và question analytics.
4. Đọc security model.
5. Dùng traceability matrix trong testing playbook.

## 5. Quy ước trạng thái tài liệu

Mỗi nhận định kỹ thuật nên thuộc một trong bốn trạng thái:

| Nhãn | Ý nghĩa |
|---|---|
| Hiện tại | Đã có trong source code đang làm việc |
| Dự kiến | Đã được thiết kế nhưng chưa chắc đã triển khai |
| Khuyến nghị | Đề xuất cải tiến, không phải hành vi runtime hiện tại |
| Không hỗ trợ | Ngoài phạm vi hoặc chưa có adapter |

Không được mô tả một khuyến nghị như thể hệ thống đã triển khai.

Không được ghi endpoint nếu chưa kiểm tra route thực tế.

Không được ghi tên cột database bằng suy đoán.

Không được ghi secret, mật khẩu hoặc API key vào tài liệu.

## 6. Quy ước ngôn ngữ

Nội dung hướng đến người dùng phải dùng tiếng Việt đầy đủ dấu.

Tên class, method, property và file giữ nguyên tiếng Anh.

Từ “AI” được dùng trên giao diện thay vì tên nhà cung cấp.

Tên nhà cung cấp chỉ dùng trong tài liệu cấu hình hoặc tích hợp backend.

Các thuật ngữ nên nhất quán:

- “Quiz” cho bài kiểm tra ôn tập.
- “Câu hỏi” cho question bank item.
- “Ngân hàng câu hỏi” cho kho câu hỏi tái sử dụng.
- “Bộ ôn tập” cho tập hoạt động được tổng hợp.
- “Lượt làm” cho một attempt.
- “Đáp án được chọn” cho selected answer.
- “Phiên bản” cho snapshot bất biến.
- “Khôi phục” cho restore.
- “Nhật ký thao tác” cho audit log.
- “Bản nháp” cho nội dung chưa phát hành.
- “Đã phát hành” cho nội dung sinh viên có thể sử dụng.

## 7. Quy ước cập nhật

Khi thêm entity mới:

- Cập nhật tài liệu database.
- Cập nhật sơ đồ quan hệ.
- Ghi delete behavior.
- Ghi dữ liệu nhạy cảm nếu có.
- Ghi chiến lược migration.

Khi thêm page mới:

- Cập nhật page inventory.
- Ghi role được phép truy cập.
- Ghi handler GET/POST.
- Ghi empty state.
- Ghi error state.
- Bổ sung test case.

Khi thêm AI capability:

- Ghi input.
- Ghi output schema.
- Ghi giới hạn.
- Ghi fallback.
- Ghi cách kiểm duyệt.
- Ghi dữ liệu nào được gửi ra ngoài.
- Ghi rate limit.

Khi thay đổi business rule:

- Cập nhật use case.
- Cập nhật acceptance criteria.
- Cập nhật test matrix.
- Xem xét tương thích dữ liệu cũ.

Khi thay đổi quyền:

- Cập nhật permission matrix.
- Kiểm tra cả UI và server.
- Kiểm tra session hết hạn.
- Kiểm tra truy cập URL trực tiếp.
- Kiểm tra POST giả mạo.

## 8. Nguyên tắc tài liệu

### 8.1. Source code là nguồn sự thật runtime

Tài liệu giải thích source code.

Tài liệu không thay thế việc đọc source code.

Nếu có xung đột, phải kiểm tra:

1. Source code đang build.
2. Schema database đang chạy.
3. Cấu hình môi trường.
4. Log runtime.
5. Tài liệu.

### 8.2. Tài liệu phải giúp ra quyết định

Một đoạn tài liệu tốt trả lời ít nhất một câu hỏi:

- Thay đổi này đặt ở đâu?
- Ai được phép thực hiện?
- Dữ liệu nào bị ảnh hưởng?
- Có thể rollback không?
- Kiểm thử thế nào?
- Theo dõi thế nào?
- Khi lỗi cần làm gì?

### 8.3. Không dùng nội dung độn

Không lặp lại cùng một câu bằng nhiều cách.

Không thêm hàng trăm dòng trống.

Không tạo danh sách không có mục đích.

Không sao chép nguyên source code dài vào tài liệu.

Không dùng văn phong quảng cáo.

Không dùng tuyên bố tuyệt đối nếu chưa có bằng chứng.

### 8.4. Ví dụ phải an toàn

Không dùng connection string thật.

Không dùng email thật.

Không dùng access token thật.

Không dùng dữ liệu sinh viên thật.

Không dùng đường dẫn máy cá nhân trong ví dụ.

## 9. Checklist review tài liệu

Trước khi merge một thay đổi tài liệu:

- [ ] Tiêu đề mô tả đúng phạm vi.
- [ ] Mục lục vẫn hợp lệ.
- [ ] Liên kết tương đối không bị hỏng.
- [ ] Tên file khớp source code.
- [ ] Thuật ngữ nhất quán.
- [ ] Tiếng Việt có dấu đầy đủ.
- [ ] Không có secret.
- [ ] Không có thông tin cá nhân.
- [ ] Không tuyên bố sai trạng thái triển khai.
- [ ] Có hướng dẫn kiểm chứng.
- [ ] Có cảnh báo cho thao tác phá hủy.
- [ ] Có ngày hoặc phiên bản nếu nội dung phụ thuộc thời gian.
- [ ] Có test hoặc acceptance criteria nếu mô tả hành vi.
- [ ] Không dùng emoji.
- [ ] Không dùng nội dung độn.

## 10. Tóm tắt

Bộ tài liệu này được tổ chức như một handbook vận hành được.

Nó không chỉ mô tả class và table.

Nó liên kết kiến trúc, business rule, bảo mật, UI, kiểm thử và vận hành.

Khi thêm tính năng, hãy cập nhật tài liệu gần với nơi ra quyết định nhất.

Khi sửa lỗi, hãy bổ sung test case hoặc troubleshooting note để lỗi không quay lại.

Khi thay đổi quyền hoặc dữ liệu, hãy ưu tiên khả năng truy vết và rollback.
