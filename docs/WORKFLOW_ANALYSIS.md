# Phân tích các workflow của RAG Edu Hub

Ngày kiểm tra: 10/07/2026  
Phạm vi: `Assignment1_Repository`, `Assignment1_Service`, `Assignmet1_Presentation` và database `rag_edu` đang được cấu hình cho project.

## Kết luận nhanh

Project đã có triển khai cả ba nhóm workflow:

| Workflow | Trạng thái | Nhận xét |
|---|---|---|
| Manager Document | Đã triển khai | Upload PDF/DOCX/PPTX, lưu file, chống trùng, trích xuất nội dung, chunk, embedding, lưu index, re-index, xóa mềm/khôi phục. |
| ChatBot tài liệu | Đã triển khai | RAG theo môn học: tạo embedding cho câu hỏi, tìm chunk bằng cosine similarity kết hợp keyword rerank, gửi top chunk vào Gemini để tạo câu trả lời và lưu citation. |
| Package Payment | Đã triển khai | Tạo checkout MoMo, callback IPN, xác thực chữ ký/số tiền, kích hoạt/gia hạn subscription, lịch sử giao dịch và báo cáo admin. |

Invoice hóa đơn điện tử riêng chưa được triển khai. Hệ thống hiện có `PaymentTicket` đóng vai trò bản ghi giao dịch/lịch sử thanh toán, không có bảng/entity/API tạo invoice hoặc xuất hóa đơn.

## 1. Manager Document

### Luồng upload

Điểm vào là `Assignmet1_Presentation/Pages/Documents/Upload.cshtml.cs`, gọi `IDocumentService.UploadAndProcessAsync(...)`.

Luồng xử lý chính:

1. Chỉ nhận `.pdf`, `.docx`, `.pptx`.
2. Kiểm tra người upload có role Lecturer và chỉ được upload vào môn học được gán.
3. Chuẩn hóa tên file, kiểm tra trùng tên trong môn học.
4. Lưu file vào `storageRoot` với tên GUID và tính SHA-256.
5. Kiểm tra trùng nội dung bằng `FileHash`.
6. Tạo bản ghi `documents` với trạng thái `processing`.
7. Trích xuất nội dung, tạo chunk, tạo embedding, lưu chunk và embedding.
8. Đổi trạng thái sang `indexed`; nếu lỗi thì đổi sang `error` và lưu `ErrorMsg`.

PDF được đọc theo từng page bằng PdfPig. DOCX được đọc từ các paragraph bằng Open XML và hiện được xem như một page. PPTX có luồng riêng: mỗi slide là một chunk, đồng thời các hình ảnh trong slide được trích xuất ra `wwwroot/slide-images/...`.

File liên quan: `Assignment1_Service/Helpers/TextExtractor.cs`, `Assignment1_Service/Helpers/SlideExtractor.cs`, `Assignment1_Service/Services/DocumentService.cs`.

### Chunk size và đơn vị chunk

Đối với PDF/DOCX, `TextChunker.Chunk(...)` dùng `string.Length` và `Substring`, vì vậy kích thước là **số ký tự C#**, không phải số từ, không phải token của LLM và cũng không phải “chữ cái” theo nghĩa từng ký tự rời nhau.

Code tạo cửa sổ tối đa bằng `start + chunkSize`, sau đó cố gắng cắt tại ranh giới dễ đọc theo thứ tự:

1. dòng trống;
2. xuống dòng;
3. dấu chấm;
4. khoảng trắng;
5. nếu không có thì cắt đúng vị trí ký tự.

Chunk có overlap để giữ ngữ cảnh giữa hai đoạn. `CharStart` và `CharEnd` được lưu trong bảng `chunks`. `TokenCount` chỉ là số phần tử tách bởi khoảng trắng để hiển thị/thống kê, không được dùng để quyết định kích thước chunk.

### Cấu hình lấy từ đâu?

`DocumentService.IndexDocumentContentAsync(...)` đọc dòng đầu tiên trong bảng `ChunkingConfigs` qua:

```text
ORDER BY Id ASC -> FirstOrDefaultAsync()
```

Đối với PDF/DOCX:

```text
chunkSize = chunkingConfig.ChunkSize ?? 800
overlap   = chunkingConfig.ChunkOverlap ?? 100
```

Do đó, ưu tiên là **database**, nhưng có fallback hardcode trong source là `800` ký tự và `100` ký tự overlap nếu bảng không có bản ghi. Code hiện không dùng trường `Strategy` hoặc `Params` để chọn thuật toán; dù database có các strategy như `fixed`, `sentence`, `recursive`, `semantic`, thực tế vẫn luôn gọi `TextChunker` hiện tại.

### Cấu hình thực tế đang có trong database

Tại thời điểm kiểm tra, database `rag_edu` có các dòng `ChunkingConfigs` sau:

| Id | Name | Strategy | ChunkSize | Overlap |
|---:|---|---|---:|---:|
| 1 | fixed-256 | fixed | 256 | 32 |
| 2 | fixed-512 | fixed | 512 | 64 |
| 3 | fixed-1024 | fixed | 1024 | 128 |
| 4 | sentence-512 | sentence | 512 | 50 |
| 5 | recursive-512 | recursive | 512 | 64 |
| 6 | semantic-512 | semantic | 512 | 50 |

Vì code lấy dòng đầu tiên, runtime hiện tại dùng **256 ký tự/chunk và overlap 32 ký tự** cho PDF/DOCX. PPTX là ngoại lệ: mỗi slide tạo một chunk, không áp dụng 256 ký tự.

## 2. ChatBot tài liệu

### Luồng RAG

`Assignment1_Service/Services/ChatService.cs` thực hiện:

1. Lấy session theo `sessionId` và `userId`.
2. Xác định môn học của session.
3. Đọc danh sách embedding model từ bảng `EmbeddingModels`.
4. Tạo embedding cho câu hỏi.
5. Quét các chunk đã index của đúng môn học theo batch 120 bản ghi.
6. Tính cosine similarity giữa vector câu hỏi và vector chunk.
7. Rerank hybrid bằng thêm điểm keyword overlap.
8. Giữ tối đa 4 chunk có điểm cao nhất.
9. Gửi câu hỏi, lịch sử gần nhất tối đa 6 message và context của các chunk vào Gemini.
10. Lưu câu trả lời, điểm similarity và citation trỏ về chunk.

`ChunkRetriever` dùng công thức cosine similarity và keyword score. Vì truy vấn lấy chunk theo `SubjectId`, chatbot không trộn tài liệu của các môn khác vào câu trả lời.

### Model trả lời

Model sinh câu trả lời là **Google Gemini**, gọi REST API `:generateContent` trong `GeminiService`.

Model mặc định trong `GeminiOptions` và `appsettings.example.json` là:

```text
gemini-2.5-flash
```

API key, model và BaseUrl lấy từ section `Gemini` trong appsettings/user-secrets. Prompt yêu cầu trả lời bằng tiếng Việt và chỉ dựa trên context đã truy xuất.

Nếu Gemini lỗi, trả về rỗng hoặc bị đánh giá là không đủ thông tin, code dùng extractive fallback nội bộ: tách câu, tokenize câu hỏi và chọn tối đa 3 câu có tỷ lệ token trùng cao nhất. Fallback này không phải LLM.

### Embedding là Gemini hay embedding nội bộ?

Code hỗ trợ hai hướng:

- Model database được nhận diện là Google/Gemini: gọi Gemini Embedding API `batchEmbedContents`, batch tối đa 100 text/lần.
- Model được nhận diện là `local`, `simple`, `internal` hoặc fallback degraded: dùng `SimpleEmbedder` nội bộ.

`SimpleEmbedder` không phải model machine learning đã huấn luyện. Nó tạo vector xác định bằng cách:

1. chuẩn hóa Unicode, bỏ dấu combining và chuyển lowercase;
2. lấy token chữ/số;
3. hash SHA-256 các feature dạng token, bigram liên tiếp và trigram ký tự;
4. đưa feature vào các vị trí của vector bằng modulo dimension;
5. chuẩn hóa vector về độ dài 1.

### Trạng thái embedding thực tế của database hiện tại

Database hiện có 4 model:

| Id | Provider | ModelId | Dimension |
|---:|---|---|---:|
| 1 | huggingface | intfloat/multilingual-e5-base | 768 |
| 2 | openai | text-embedding-3-small | 1536 |
| 3 | huggingface | vinai/phobert-base | 768 |
| 4 | huggingface | BAAI/bge-m3 | 1024 |

Code hiện không có client HuggingFace hoặc OpenAI embedding. Các model trên cũng không được `EmbeddingModelSelector` nhận diện là Gemini/local. Vì vậy `ResolveForExecution(...)` đưa chúng vào degraded fallback và runtime thực tế dùng `SimpleEmbedder` nội bộ cho cả 4 dòng, với dimension tương ứng.

Kết luận: project **có hỗ trợ đường gọi Gemini embedding**, nhưng với database hiện tại thì chatbot đang chạy bằng **embedding nội bộ dạng hashing**, không phải HuggingFace, OpenAI hay Gemini embedding. `Gemini:EmbeddingModel = gemini-embedding-001` trong appsettings chỉ là giá trị mặc định/fallback tên model; model thực thi chính vẫn do các dòng trong `EmbeddingModels` quyết định.

## 3. Package Payment, Report và Invoice

### Thanh toán MoMo

Điểm vào UI là `Assignmet1_Presentation/Pages/MoMoPayment/Checkout.cshtml.cs`. Service `MomoPaymentService` thực hiện:

1. Lấy plan đang active từ database.
2. Tạo `PaymentTicket` trạng thái `momo_pending`.
3. Tạo `orderId`, `requestId`, amount và `extraData` chứa `userId/planId`.
4. Tạo chữ ký HMAC SHA-256 theo secret key.
5. Gọi endpoint MoMo create payment.
6. Lưu response, pay URL và result code vào ticket.
7. Chuyển student sang pay URL của MoMo.

`MoMoPayment/Ipn` nhận callback server-to-server. Code kiểm tra order tồn tại, số tiền khớp và chữ ký hợp lệ trước khi hoàn tất ticket. `MoMoPayment/Return` có fallback cho local test không nhận được IPN: nếu MoMo trả `resultCode == 0` và ticket còn `momo_pending`, hệ thống vẫn kích hoạt subscription.

### Kích hoạt subscription

`SubscriptionService.CompleteTicketAsync(...)` tạo `UserSubscription` từ payment ticket, đặt thời hạn theo `duration_days`, sau đó đổi ticket thành `approved`. Nếu student đang có subscription còn hạn, gói mới nối tiếp từ `current.EndAt`; nếu không, bắt đầu từ thời điểm hiện tại.

Các bảng chính:

- `subscription_plans`: danh sách gói và giá/thời hạn;
- `payment_tickets`: giao dịch, trạng thái, mã MoMo và callback JSON;
- `user_subscriptions`: quyền sử dụng sau khi thanh toán thành công;
- `student_chat_usages`: quota Free theo student/môn học/cửa sổ thời gian.

### Lịch sử giao dịch

Student xem lịch sử riêng của mình qua `GetUserTicketsAsync(userId)` và repository lọc `Where(t => t.UserId == userId)`. Admin xem toàn bộ qua `GetAllTicketsAsync(...)`, có thể lọc trạng thái.

### Báo cáo admin

Trang `Subscription/Index` khi role là admin lấy các ticket `approved`, lọc `fromDate/toDate`, rồi tính:

- tổng doanh thu;
- tổng số đơn thành công;
- gói bán nhiều nhất;
- giá trị đơn trung bình;
- số lượt mua, doanh thu và tỷ lệ theo từng gói.

Phần lọc và group báo cáo hiện thực hiện trong memory sau khi lấy danh sách ticket approved; chưa phải một truy vấn aggregate trực tiếp tại SQL Server. Báo cáo hiện là báo cáo theo khoảng thời gian và theo plan, chưa có bảng hóa đơn/đối soát riêng.

### Invoice

Không tìm thấy entity/table/service/page/API riêng cho `Invoice`, `HoaDon`, `Receipt` hoặc xuất PDF hóa đơn. `PaymentTicket` chỉ lưu thông tin giao dịch và JSON callback MoMo. Nếu cần invoice thật, project còn thiếu ít nhất model/database table, số hóa đơn, thông tin người mua, trạng thái phát hành và endpoint tải/xuất hóa đơn.

## Các file nguồn quan trọng

- Upload/chunk: `Assignment1_Service/Services/DocumentService.cs`, `Assignment1_Service/Helpers/TextChunker.cs`, `Assignment1_Service/Helpers/TextExtractor.cs`.
- Cấu hình chunk/model: `Assignment1_Repository/Repositories/DocumentRepository.cs`, `Assignment1_Repository/Repositories/ChatRepository.cs`, `Assignment1_Repository/Models/ChunkingConfig.cs`, `Assignment1_Repository/Models/EmbeddingModel.cs`.
- Embedding/RAG: `Assignment1_Service/Services/EmbeddingService.cs`, `Assignment1_Service/Helpers/SimpleEmbedder.cs`, `Assignment1_Service/Helpers/EmbeddingModelSelector.cs`, `Assignment1_Service/Helpers/ChunkRetriever.cs`, `Assignment1_Service/Services/ChatService.cs`.
- LLM: `Assignment1_Service/Services/GeminiService.cs`, `Assignmet1_Presentation/appsettings.example.json`.
- Payment/subscription: `Assignment1_Service/Services/MomoPaymentService.cs`, `Assignment1_Service/Services/SubscriptionService.cs`, `Assignmet1_Presentation/Pages/MoMoPayment`, `Scripts/CreateSubscriptionTables.sql`.
- Admin report: `Assignmet1_Presentation/Pages/Subscription/Index.cshtml.cs`, `Assignmet1_Presentation/Models/AdminSubscriptionPlanReportViewModel.cs`.

## Rủi ro/khoảng trống kỹ thuật cần biết

1. `ChunkingConfig.Strategy` đang được lưu nhưng chưa được dùng để chọn các thuật toán sentence/recursive/semantic.
2. Chỉ lấy config có `Id` nhỏ nhất, chưa có cờ active/default để quản trị nhiều cấu hình.
3. Các provider embedding HuggingFace/OpenAI trong database chưa có adapter thực tế; hiện bị chuyển thành simple hashing fallback.
4. Fallback sinh câu trả lời khi Gemini lỗi là extractive, chất lượng phụ thuộc keyword overlap.
5. MoMo return fallback hữu ích cho local test nhưng trong production cần dựa chính vào IPN và kiểm soát chặt endpoint public.
6. Báo cáo admin hiện tính trong application memory và chưa có invoice điện tử riêng.
