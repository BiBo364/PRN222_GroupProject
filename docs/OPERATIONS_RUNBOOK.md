# Runbook vận hành RAG Edu Hub

## 1. Mục tiêu

Runbook cung cấp quy trình thao tác lặp lại cho môi trường local, test và production.

Người dùng mục tiêu:

- Developer.
- Tester.
- DevOps.
- Người trực sự cố.
- Quản trị hệ thống.

Mọi lệnh production phải được điều chỉnh theo hạ tầng thực tế.

Không chạy lệnh xóa dữ liệu chỉ vì nó xuất hiện trong tài liệu.

## 2. System components

Thành phần bắt buộc:

- .NET 8 runtime.
- ASP.NET Core application.
- SQL Server.
- Storage cho document.

Thành phần tích hợp:

- AI provider.
- SMTP.
- MoMo.
- CDN asset.

## 3. Local prerequisites

Kiểm tra:

```powershell
dotnet --info
```

Yêu cầu:

- .NET SDK phù hợp.
- SQL Server instance chạy.
- Database tồn tại.
- Connection string hợp lệ.
- Port ứng dụng trống.

Tùy chức năng:

- Node/npm cho browser automation.
- Visual Studio.
- SQL Server Management Studio.

## 4. Configuration

### 4.1. File template

Sao chép:

```powershell
Copy-Item `
  Assignmet1_Presentation\appsettings.example.json `
  Assignmet1_Presentation\appsettings.json
```

Không commit file chứa secret.

### 4.2. Connection string

Ví dụ Windows Authentication:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=rag_edu;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Ví dụ chỉ là placeholder.

Không dùng credential production trong docs.

### 4.3. AI

Section cấu hình cần:

- API key.
- Model.
- Base URL nếu có.
- Timeout.
- Embedding model nếu dùng.

UI gọi là AI.

Backend có thể dùng tên provider.

### 4.4. MoMo

Cấu hình:

- Partner code.
- Access key.
- Secret key.
- Endpoint.
- Redirect URL.
- IPN URL.

Local IPN có thể cần tunnel.

Không đánh giá payment production chỉ bằng redirect return.

### 4.5. SMTP

Cấu hình:

- Host.
- Port.
- Username.
- Password.
- Sender.
- TLS.

## 5. Build

### 5.1. Restore

```powershell
dotnet restore Assignmet1.sln
```

### 5.2. Build

```powershell
dotnet build Assignmet1.sln
```

### 5.3. Không restore lại

```powershell
dotnet build Assignmet1.sln --no-restore
```

### 5.4. Build failure checklist

- Kiểm tra SDK.
- Kiểm tra NuGet source.
- Kiểm tra file bị process khóa.
- Kiểm tra syntax.
- Kiểm tra package version.
- Kiểm tra connection không cần thiết ở build time.

## 6. Run

### 6.1. Chạy project

```powershell
dotnet run --project Assignmet1_Presentation
```

### 6.2. Chọn URL

```powershell
dotnet run `
  --project Assignmet1_Presentation `
  --urls http://127.0.0.1:5099
```

### 6.3. Content root

Chạy từ đúng thư mục để app đọc appsettings.

Nếu chạy DLL từ thư mục khác:

- Content root có thể sai.
- Appsettings không được tìm.
- Storage path có thể sai.

## 7. Startup sequence

Khi start:

1. Load configuration.
2. Validate connection string.
3. Register services.
4. Build app.
5. Synchronize schema mở rộng nếu có.
6. Build middleware pipeline.
7. Bind port.
8. Health endpoint sẵn sàng.

Nếu startup fail:

- Đọc exception đầu tiên.
- Không chỉ nhìn exception cuối.
- Kiểm tra config path.
- Kiểm tra SQL connectivity.

## 8. Health checks

### 8.1. Liveness

Endpoint:

```text
/health/live
```

Ý nghĩa:

- Process đang chạy.
- Pipeline phản hồi.

Không bảo đảm SQL hoạt động.

### 8.2. Readiness

Endpoint:

```text
/health/ready
```

Ý nghĩa:

- Application sẵn sàng.
- Database kết nối được.

### 8.3. Kiểm tra PowerShell

```powershell
Invoke-WebRequest `
  -UseBasicParsing `
  http://127.0.0.1:5099/health/ready
```

### 8.4. Status

- HTTP 200: healthy.
- HTTP 503: unhealthy/degraded tùy cấu hình.

### 8.5. Probe design

Orchestrator:

- Liveness failure: restart process.
- Readiness failure: ngừng route traffic.

Không dùng readiness failure đơn lẻ để xóa pod ngay.

## 9. Smoke test

Sau deploy:

- [ ] `/health/live` 200.
- [ ] `/health/ready` 200.
- [ ] Login page render.
- [ ] Static CSS load.
- [ ] Font/icon load.
- [ ] Login test account.
- [ ] Home load.
- [ ] Documents list load.
- [ ] Learning hub load.
- [ ] Admin page đúng quyền.
- [ ] Audit page đúng quyền.
- [ ] Logout.

Không dùng tài khoản hoặc dữ liệu production thật cho smoke destructive.

## 10. Database connectivity incident

### 10.1. Triệu chứng

- Ready unhealthy.
- Login/query fail.
- SQL exception.
- Timeout.
- Startup fail.

### 10.2. Kiểm tra

1. SQL service có chạy?
2. Host resolve được?
3. Port mở?
4. Database tồn tại?
5. User có quyền?
6. Certificate/trust setting đúng?
7. Connection pool cạn?
8. Database đang restore?

### 10.3. Không làm

- Không in full connection string vào log.
- Không paste password vào chat công khai.
- Không restart SQL liên tục.
- Không xóa database để thử.

### 10.4. Recovery

- Khôi phục network.
- Khởi động SQL nếu được phép.
- Sửa secret.
- Scale down request nếu quá tải.
- Kiểm tra ready.
- Chạy smoke.

## 11. AI incident

### 11.1. Triệu chứng

- Generate timeout.
- HTTP 401/403 từ provider.
- HTTP 429.
- JSON invalid.
- Response rỗng.
- Chất lượng thấp.

### 11.2. Phân loại

Authentication:

- API key.
- Project permission.

Quota:

- Provider quota.
- Application rate limit.

Network:

- DNS.
- TLS.
- Proxy.

Format:

- Model response.
- Prompt.
- Parser.

Quality:

- Context.
- Source.
- Temperature/config.

### 11.3. Recovery

- Không xóa draft.
- Kiểm tra local rate limit.
- Kiểm tra provider status.
- Rotate key nếu compromise.
- Giảm context nếu payload lớn.
- Dùng fallback nếu có.
- Cho lecturer retry.

### 11.4. Không làm

- Không disable validation.
- Không trả raw AI output.
- Không log key.
- Không tăng retry vô hạn.

## 12. HTTP 429 incident

### 12.1. Xác định limiter

Có thể là:

- Global limiter.
- AI generation policy.
- Quiz submission policy.
- Provider limiter.

### 12.2. Dấu hiệu local limiter

- Response tiếng Việt từ app.
- Retry-After.
- Pattern đúng window.

### 12.3. Dấu hiệu provider limiter

- App nhận error từ provider.
- Local request vẫn vào handler.

### 12.4. Hành động

- Xác định actor/IP.
- Kiểm tra có bot không.
- Không tăng limit ngay.
- Đo nhu cầu hợp lệ.
- Điều chỉnh có test.

## 13. HTTP 500 incident

### 13.1. Thu thập

- Thời gian.
- Route.
- User role.
- Trace identifier.
- Reproduction steps.
- App log.
- Database state.

### 13.2. Điều tra

1. Tìm trace ID.
2. Xác định exception type.
3. Xác định thay đổi gần nhất.
4. Kiểm tra dependency.
5. Kiểm tra input.
6. Kiểm tra data invariant.

### 13.3. Recovery

- Rollback build nếu regression.
- Sửa config nếu sai.
- Khôi phục dependency.
- Data repair bằng transaction nếu cần.

## 14. Audit incident

### 14.1. Không có log

Kiểm tra:

- Request có mutating method?
- Role là staff?
- Route có bị exclude?
- Service audit đăng ký?
- Database table tồn tại?
- Middleware order?

### 14.2. Log sai actor

Kiểm tra:

- Session.
- Login/logout.
- Shared browser.
- User ID capture.

### 14.3. Log quá nhiều

Kiểm tra:

- Autosave bị audit?
- Polling dùng POST?
- Retry loop?

## 15. Quiz analytics incident

### 15.1. Dashboard zero

Kiểm tra:

- Date range.
- Subject.
- Quiz filter.
- Attempt completed.
- Timezone.
- Test data đã bị xóa.

### 15.2. Average sai

Kiểm tra:

- Score scale.
- Total points zero.
- Percentage conversion.
- Attempt duplicate.

### 15.3. Option percentage sai

Kiểm tra:

- Denominator.
- Answer normalization.
- Option JSON.
- Shuffle mapping.
- Missing answers.

### 15.4. Duration bất thường

Kiểm tra:

- Started/completed.
- UTC/local.
- Attempt bỏ quên.
- Outlier exclusion.

## 16. Version restore incident

### 16.1. Restore fail

Kiểm tra:

- Version tồn tại.
- Ownership.
- Snapshot parse.
- Snapshot schema.
- Foreign key.
- Transaction.

### 16.2. Quiz bị publish sau restore

Expected:

Restore phải về draft.

Nếu không:

- Đây là security/product bug.
- Unpublish ngay.
- Kiểm tra service.
- Ghi incident.

### 16.3. Attempt mất

Đây là incident nghiêm trọng.

Hành động:

- Dừng thao tác tiếp.
- Backup hiện trạng.
- Kiểm tra hard delete.
- Restore DB nếu cần.
- So sánh version.

## 17. Document indexing incident

### 17.1. Processing mãi

Kiểm tra:

- Worker/process.
- Parser.
- File size.
- Database lock.
- Embedding call.

### 17.2. Error status

Đọc `error_msg` an toàn.

Không hiển thị stack trace cho user.

### 17.3. Duplicate

Kiểm tra:

- Filename.
- File hash.
- Subject scope.

### 17.4. Re-index

Re-index phải:

- Xác định document.
- Xóa/thay chunk cũ an toàn.
- Tạo embedding nhất quán.
- Chuyển status.

## 18. Storage incident

### 18.1. File missing

Triệu chứng:

- Metadata có.
- Download/view fail.

Kiểm tra:

- Storage path.
- Volume mount.
- Permission.
- Backup restore.

### 18.2. Disk full

Hành động:

- Dừng upload mới nếu cần.
- Xác định thư mục lớn.
- Không xóa file không đối chiếu DB.
- Mở rộng volume.
- Chạy cleanup theo policy.

### 18.3. Orphan file

Không xóa tự động trước khi:

- Snapshot danh sách.
- Đối chiếu DB.
- Có retention.
- Có backup.

## 19. Payment incident

### 19.1. Pending lâu

Kiểm tra:

- IPN reachability.
- Provider status.
- Signature.
- Order.
- Return callback.

### 19.2. Approved nhưng chưa có subscription

Kiểm tra:

- Transaction.
- Idempotency.
- Foreign key.
- Service log.

### 19.3. Duplicate subscription

Kiểm tra:

- Callback lặp.
- Unique rule.
- CompleteTicket idempotency.

Không tự xóa payment record.

## 20. Backup

### 20.1. Database

Backup:

- Full.
- Differential nếu cần.
- Transaction log nếu dùng full recovery.

### 20.2. Storage

Backup document storage.

Giữ mapping với DB backup time.

### 20.3. Configuration

Backup:

- Non-secret config.
- Secret version reference.
- Deployment manifest.

Không export secret vào Git.

### 20.4. Verification

Backup chưa được coi thành công nếu chưa test restore.

## 21. Restore

### 21.1. Chuẩn bị

- Xác định RPO.
- Xác định RTO.
- Chọn restore point.
- Dừng ghi nếu cần.
- Snapshot hiện trạng.

### 21.2. Database restore

- Restore vào instance kiểm tra trước.
- Chạy consistency check.
- Kiểm tra schema.
- Kiểm tra user count.
- Kiểm tra learning data.

### 21.3. Storage restore

- Khớp thời điểm.
- Khớp path.
- Khớp permission.

### 21.4. App

- Deploy build tương thích schema.
- Ready check.
- Smoke test.
- Mở traffic.

## 22. Deployment checklist

### Before

- [ ] Code review.
- [ ] Build pass.
- [ ] Test pass.
- [ ] Config present.
- [ ] Secret present.
- [ ] Database backup.
- [ ] Migration reviewed.
- [ ] Rollback plan.
- [ ] Maintenance window nếu cần.

### During

- [ ] Apply schema.
- [ ] Deploy app.
- [ ] Check process.
- [ ] Check logs.
- [ ] Check liveness.
- [ ] Check readiness.
- [ ] Run smoke.

### After

- [ ] Monitor 5xx.
- [ ] Monitor latency.
- [ ] Monitor 429.
- [ ] Monitor SQL.
- [ ] Monitor AI error.
- [ ] Monitor audit writes.
- [ ] Verify key user journey.

## 23. Rollback

Rollback application:

- Deploy previous artifact.
- Không build lại từ branch mơ hồ.

Rollback database:

- Chỉ khi migration rollback an toàn.
- Có thể cần forward fix.

Rollback AI config:

- Trở về model/config đã biết.
- Không lộ key.

## 24. Monitoring recommendations

Metrics nên có:

- Request rate.
- Error rate.
- Latency percentiles.
- Active sessions.
- SQL connection errors.
- AI latency.
- AI failures.
- AI 429.
- Local 429.
- Quiz submissions.
- Audit write failures.
- Document indexing queue.
- Storage usage.
- Payment pending.

## 25. Alert recommendations

Alert khi:

- Ready fail liên tục.
- 5xx tăng.
- SQL timeout tăng.
- AI error tăng.
- Audit write fail.
- Disk gần đầy.
- Payment mismatch.
- Login failure burst.

Alert phải:

- Có owner.
- Có severity.
- Có runbook link.
- Không quá nhiễu.

## 26. Incident process

### Detect

Alert hoặc user report.

### Triage

Xác định:

- Severity.
- Scope.
- Start time.
- Affected users.

### Contain

- Disable feature nếu cần.
- Rate limit.
- Rollback.
- Block compromised key.

### Recover

- Fix.
- Restore.
- Verify.

### Learn

- Postmortem.
- Test regression.
- Update runbook.
- Update monitoring.

## 27. Postmortem template

### Tiêu đề

Mô tả ngắn sự cố.

### Thời gian

- Bắt đầu.
- Phát hiện.
- Giảm thiểu.
- Kết thúc.

### Ảnh hưởng

- Người dùng.
- Dữ liệu.
- Thời gian.

### Timeline

Các mốc có bằng chứng.

### Root cause

Nguyên nhân kỹ thuật và quy trình.

### Contributing factors

Điều kiện làm sự cố nghiêm trọng hơn.

### Resolution

Cách khôi phục.

### Action items

Mỗi item có:

- Owner.
- Deadline.
- Priority.
- Verification.

## 28. Log handling

Không đưa vào log:

- Password.
- API key.
- Full connection string.
- Card/payment secret.
- Token.

Khi chia sẻ log:

- Redact.
- Giữ trace.
- Giữ timestamp.
- Giữ event.

## 29. Capacity planning

Theo dõi:

- User growth.
- Document size.
- Chunk count.
- Embedding storage.
- Message volume.
- Attempt volume.
- Answer volume.
- Version snapshot size.
- Audit volume.

Version và audit tăng liên tục.

Cần retention/archival trước khi quá lớn.

## 30. Maintenance

Hàng ngày:

- Health.
- Error.
- Disk.
- Payment pending.

Hàng tuần:

- Backup verification.
- Audit failure.
- Indexing error.
- AI cost.

Hàng tháng:

- Restore drill.
- Dependency update review.
- Permission review.
- Capacity review.
- Data quality.

## 31. Definition of ready for production

- [ ] HTTPS.
- [ ] Secret manager.
- [ ] Backup.
- [ ] Restore tested.
- [ ] Health probes.
- [ ] Central logs.
- [ ] Rate limiting.
- [ ] Security headers.
- [ ] Error handler.
- [ ] Audit.
- [ ] Monitoring.
- [ ] Alert.
- [ ] Runbook.
- [ ] On-call owner.

## 32. Kết luận

Vận hành tốt dựa trên quy trình có thể lặp lại.

Health check cho biết hệ thống có sẵn sàng.

Trace identifier giúp nối user report với log.

Audit giúp xác định thao tác.

Backup và version giúp khôi phục.

Rate limiting giúp hệ thống chịu được lạm dụng.

Runbook phải được cập nhật sau mỗi sự cố thực tế.
