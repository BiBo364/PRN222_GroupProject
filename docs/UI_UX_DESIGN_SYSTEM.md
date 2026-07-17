# Design system và quy chuẩn UI/UX

## 1. Mục tiêu

Tài liệu giúp giao diện RAG Edu Hub nhất quán, dễ dùng và dễ bảo trì.

Thiết kế hướng đến:

- Sinh viên.
- Giảng viên.
- Quản trị viên.
- Desktop.
- Tablet.
- Mobile.

Phong cách:

- Hiện đại.
- Học thuật.
- Rõ ràng.
- Có chiều sâu.
- Không phô trương.
- Không dùng emoji.
- Không dùng hiệu ứng gây nhiễu.

## 2. Product experience principles

### 2.1. Rõ trước, đẹp sau

Người dùng phải hiểu:

- Đang ở đâu.
- Có thể làm gì.
- Thao tác có thành công không.
- Dữ liệu đã lưu chưa.
- Cách quay lại.

### 2.2. Một trang một nhiệm vụ chính

Mỗi page có:

- Một heading chính.
- Một CTA chính.
- Các action phụ có mức ưu tiên thấp hơn.

### 2.3. Trạng thái luôn hiển thị

Đặc biệt:

- Draft.
- Published.
- Deleted.
- Saving.
- Saved.
- Offline.
- Error.
- Empty.
- Loading.

### 2.4. Motion có mục đích

Motion dùng để:

- Xác nhận tương tác.
- Dẫn focus.
- Thể hiện hierarchy.
- Chuyển trạng thái.

Motion không dùng chỉ để trang “trông nhiều hiệu ứng”.

## 3. Brand foundation

### 3.1. Tên

Tên hiển thị:

```text
RAG Edu Hub
```

### 3.2. Giọng văn

Giọng văn:

- Tôn trọng.
- Ngắn gọn.
- Chủ động.
- Không đổ lỗi.
- Không robot.

### 3.3. Từ ngữ

Dùng:

- Tạo câu hỏi bằng AI.
- Bản nháp.
- Đã phát hành.
- Khôi phục.
- Nhật ký thao tác.
- Lượt làm.

Tránh:

- Tên provider ở CTA.
- Technical exception.
- Từ tiếng Anh không cần thiết.
- Dấu câu thiếu.

## 4. Color system

### 4.1. Primary

Primary dùng cho:

- CTA.
- Active navigation.
- Link quan trọng.
- Focus accent.

Hệ màu hiện thiên về:

- Indigo.
- Violet.
- Blue.

### 4.2. Neutral

Neutral dùng cho:

- Text.
- Border.
- Background.
- Disabled.

### 4.3. Semantic

Success:

- Save thành công.
- Publish thành công.

Warning:

- Draft chưa lưu.
- Sắp hết thời gian.

Danger:

- Delete.
- Error.
- Invalid.

Info:

- Hướng dẫn.
- Trạng thái phụ.

### 4.4. Contrast

Text normal cần contrast đủ.

Không dùng gray quá nhạt trên white.

Button disabled vẫn phải đọc được.

## 5. Typography

### 5.1. Font

Font sans-serif hiện đại.

Fallback system font phải hoạt động khi CDN lỗi.

### 5.2. Scale

Các cấp:

- Display.
- Page title.
- Section title.
- Card title.
- Body.
- Small.
- Caption.

### 5.3. Line length

Paragraph dài:

- Khoảng 60-80 ký tự một dòng trên desktop.

Không kéo text toàn màn hình.

### 5.4. Vietnamese

Font phải hỗ trợ:

- Đ.
- ă.
- â.
- ê.
- ô.
- ơ.
- ư.
- Dấu kết hợp.

## 6. Spacing

Sử dụng thang khoảng cách nhất quán.

Ví dụ:

- 4.
- 8.
- 12.
- 16.
- 20.
- 24.
- 32.
- 48.
- 64.

Không đặt giá trị ngẫu nhiên nếu token đủ dùng.

## 7. Radius

Radius nhỏ:

- Input.
- Badge.

Radius vừa:

- Button.
- Mini card.

Radius lớn:

- Surface card.
- Hero.
- Modal.

Không dùng quá nhiều kiểu radius trên một trang.

## 8. Shadows

Shadow nhẹ:

- Card.
- Dropdown.

Shadow vừa:

- Sticky header.
- Modal.

Không dùng shadow đen nặng.

Shadow phải nhất quán với background.

## 9. Layout

### 9.1. App shell

Gồm:

- Header.
- Main.
- Footer.

Header giữ navigation.

Main có max width và padding.

Footer không tranh attention.

### 9.2. Content container

Desktop:

- Center.
- Max width.
- Có gutter.

Mobile:

- Gutter 12-16px.
- Không ép card quá rộng.

### 9.3. Grid

Dashboard:

- Desktop nhiều cột.
- Tablet hai cột.
- Mobile một cột.

## 10. Navigation

### 10.1. Desktop

Hiển thị:

- Logo.
- Primary links.
- Account trigger.

### 10.2. Mobile

Hiển thị:

- Logo.
- Menu toggle.
- Collapsible nav.

### 10.3. Active state

Active link:

- Màu primary.
- Background nhẹ.
- Không chỉ dựa màu nếu có thể.

### 10.4. Account menu

Account menu chứa:

- Tên.
- Role.
- Action phù hợp.
- Logout.

Menu phải:

- Nằm trên content.
- Không bị hero che.
- Bấm được.
- Đóng khi click ngoài.

## 11. Page header

Page header có thể gồm:

- Eyebrow.
- H1.
- Description.
- Primary action.

H1 chỉ một.

Description giải thích giá trị, không lặp H1.

## 12. Hero

Hero dùng ở trang tổng quan.

Không dùng hero lớn ở mọi CRUD page.

Hero có:

- Eyebrow.
- Headline.
- Supporting text.
- CTA.
- Visual phụ.

Trên mobile:

- Headline giảm.
- Visual có thể ẩn.
- CTA stack.

## 13. Cards

### 13.1. Surface card

Dùng cho:

- Filter.
- Form.
- Dashboard section.
- Empty state.

### 13.2. Metric card

Gồm:

- Icon.
- Label.
- Value.
- Context.

Không thêm chart nhỏ nếu không mang thông tin.

### 13.3. Interactive card

Hover:

- Translate nhẹ.
- Border primary nhẹ.
- Shadow tăng nhẹ.

Focus keyboard tương đương hover.

## 14. Buttons

### 14.1. Primary

Một primary CTA trong section.

### 14.2. Secondary

Dùng cho action không chính.

### 14.3. Destructive

Danger rõ.

Có confirm khi:

- Delete.
- Disable.
- Restore overwrite.
- Unpublish quan trọng.

### 14.4. Loading

Khi submit:

- Disable.
- Hiển thị spinner/text.
- Tránh double click.

## 15. Forms

### 15.1. Label

Mọi input có label visible.

Placeholder không thay label.

### 15.2. Helper text

Helper giải thích format hoặc tác động.

### 15.3. Validation

Validation:

- Gần field.
- Tiếng Việt.
- Cụ thể.
- Không chỉ màu.

### 15.4. Long form

Chia section:

- Thông tin chung.
- Cấu hình.
- Nội dung.
- Publish.

### 15.5. Required

Required indicator nhất quán.

Không gắn required cho field optional.

## 16. Tables

### 16.1. Header

Header rõ.

Text alignment theo type:

- Text trái.
- Number phải.
- Status giữa hoặc trái.

### 16.2. Row action

Action:

- Không quá nhiều icon không label.
- Destructive tách.
- Mobile vẫn truy cập.

### 16.3. Responsive

Chiến lược:

- Horizontal container scroll.
- Priority columns.
- Card transformation.

Không để body page overflow.

### 16.4. Empty table

Hiển thị:

- Lý do.
- Cách tạo dữ liệu.
- Reset filter nếu cần.

## 17. Filters

Filter card:

- Label.
- Control.
- Apply.
- Reset.

Desktop:

- Inline/grid.

Mobile:

- Stack.
- Button full width nếu phù hợp.

Filter state phản ánh URL khi có thể.

## 18. Status badges

Badge dùng cho:

- Role.
- Draft.
- Published.
- Difficulty.
- Active.
- Deleted.

Badge text luôn hiển thị.

Không chỉ dùng dot màu.

## 19. Modal

### 19.1. Layering

Modal phải ở stacking context cao.

Backdrop dưới modal.

SweetAlert cao hơn Bootstrap modal nếu cần.

### 19.2. Content

Modal confirm:

- Title.
- Hành động.
- Entity.
- Hậu quả.
- Cancel.
- Confirm.

### 19.3. Focus

- Focus trap.
- Escape.
- Close button label.
- Return focus.

## 20. Toast và alerts

Success:

- Nói kết quả.

Error:

- Nói việc chưa hoàn tất.
- Hướng dẫn tiếp.

Không dùng:

- “Something went wrong”.
- Raw exception.
- Provider message.

## 21. Empty states

Empty state tốt:

- Icon phù hợp.
- Heading.
- Explanation.
- CTA.

Ví dụ dashboard không có attempt:

- “Chưa có dữ liệu làm bài”.
- “Dashboard sẽ hiển thị sau khi sinh viên hoàn thành Quiz”.

## 22. Loading states

### 22.1. Page

Server-rendered page có thể dùng browser loading tự nhiên.

### 22.2. AJAX

Dùng:

- Spinner nhỏ.
- Skeleton nếu chờ dài.
- Disable control.

Không làm layout nhảy mạnh.

## 23. Error states

Trang Error:

- Status code.
- Heading.
- Description.
- Trace ID.
- Home.
- Reload.

Không:

- Stack trace.
- Database server.
- File path nội bộ.

## 24. Quiz editor

### 24.1. Structure

- Top metadata.
- Save status.
- Question list.
- Add action.
- Publish action.

### 24.2. Question card

Gồm:

- Number.
- Type.
- Prompt.
- Options.
- Correct marker.
- Explanation.
- Points.
- Reorder.
- Duplicate.
- Delete.

### 24.3. Autosave indicator

Trạng thái:

- Chưa lưu.
- Đang lưu.
- Đã lưu lúc.
- Lưu trên thiết bị.
- Không thể đồng bộ.

## 25. Quiz player

### 25.1. Focus

Question là trọng tâm.

Không để navigation quá nổi.

### 25.2. Progress

Hiển thị:

- Câu hiện tại.
- Tổng.
- Đã trả lời.

### 25.3. Timer

Timer rõ nhưng không gây hoảng.

Warning khi gần hết.

### 25.4. Submit

Submit có confirm nếu còn câu trống.

## 26. Analytics

### 26.1. Metric cards

Value lớn.

Label rõ.

Context nhỏ.

### 26.2. Trend

Chart có:

- Accessible name.
- Date.
- Attempts.
- Average.

### 26.3. Question analysis

Question card có:

- Difficulty.
- Sample size.
- Prompt.
- Correct rate.
- Option bars.

Mỗi bar có số và phần trăm text.

### 26.4. Mobile

- Metric stack.
- Filter stack.
- Chart container xử lý overflow.
- Table scroll.

## 27. Audit log

Mỗi event hiển thị:

- Description.
- Category.
- Action.
- Entity.
- Time.
- Actor.
- Method/path.
- Status.
- IP nếu được phép.

Log list cần scan nhanh.

## 28. Motion

Duration gợi ý:

- Micro: 120-180ms.
- Component: 180-260ms.
- Page decorative: 300-500ms.

Easing:

- Ease-out khi vào.
- Ease-in khi ra.

Tôn trọng:

```css
@media (prefers-reduced-motion: reduce)
```

Không animate:

- Layout lớn liên tục.
- Text đọc.
- Timer mỗi giây bằng scale.

## 29. Hover

Hover phù hợp:

- Card nâng nhẹ.
- Button đổi gradient nhẹ.
- Link underline/translate icon.

Không:

- Rotate mạnh.
- Blur text.
- Scale làm layout dịch.

## 30. Accessibility

### 30.1. Semantic HTML

Dùng:

- `nav`.
- `main`.
- `section`.
- `article`.
- `table`.
- `button`.

Không dùng div clickable khi button phù hợp.

### 30.2. Heading

- Một H1.
- H2 theo section.
- Không bỏ cấp tùy tiện.

### 30.3. Keyboard

Mọi action dùng được bằng keyboard.

### 30.4. Screen reader

Icon-only button có label.

Chart có description.

Status động có live region khi phù hợp.

### 30.5. Reduced motion

Tắt motion không cần thiết.

## 31. Responsive breakpoints

Không phụ thuộc chỉ thiết bị cụ thể.

Kiểm tra ít nhất:

- 390px.
- 768px.
- 1024px.
- 1366px.
- 1920px.

## 32. Content guidelines

### 32.1. Button labels

Dùng động từ:

- Tạo Quiz.
- Lưu bản nháp.
- Phát hành.
- Khôi phục.
- Áp dụng.

### 32.2. Confirm

Nói rõ:

- “Vô hiệu hóa tài khoản”.
- Không chỉ “Xác nhận”.

### 32.3. Error

Mẫu:

```text
Không thể lưu bản nháp. Nội dung vẫn được giữ trên thiết bị. Vui lòng kiểm tra kết nối và thử lại.
```

### 32.4. Success

Mẫu:

```text
Đã lưu bản nháp.
```

## 33. UI review checklist

- [ ] Một H1.
- [ ] CTA chính rõ.
- [ ] Label đầy đủ.
- [ ] Validation tiếng Việt.
- [ ] Empty state.
- [ ] Loading state.
- [ ] Error state.
- [ ] Success state.
- [ ] Keyboard.
- [ ] Focus.
- [ ] Contrast.
- [ ] Mobile 390px.
- [ ] Tablet.
- [ ] Desktop.
- [ ] Modal layering.
- [ ] Account menu.
- [ ] Không console error.
- [ ] Không emoji.
- [ ] Không lộ tên provider ở UI.

## 34. Definition of done UI

UI hoàn tất khi:

- Chức năng đúng.
- Trạng thái rõ.
- Không bị che.
- Không overflow ngoài ý muốn.
- Responsive.
- Accessible cơ bản.
- Tiếng Việt đầy đủ.
- Không lỗi console.
- Motion có reduced mode.
- Error không leak.

## 35. Kết luận

Giao diện chuyên nghiệp không đến từ số lượng animation.

Nó đến từ:

- Hierarchy.
- Consistency.
- Feedback.
- Accessibility.
- Nội dung rõ.
- Trạng thái đầy đủ.

Motion và hover chỉ củng cố những yếu tố đó.
