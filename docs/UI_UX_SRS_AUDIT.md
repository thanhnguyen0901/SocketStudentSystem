# UI/UX SRS Audit

Date: 2026-03-30
Last updated: 2026-03-30
Scope: đối chiếu `docs/SRS.md` với codebase theo luồng UI/UX chính của client-server.
Status legend: `OPEN`, `IN_PROGRESS`, `FIXED`, `WONT_FIX`

## Scope reviewed

- TCP connect flow
- Database connect flow
- Student add flow
- Results query flow
- Client state/validation behavior
- Server behavior ảnh hưởng trực tiếp tới UI/UX flow

## Summary

- Luồng tổng thể hiện có: `TCP Connect -> DbConnect -> StudentAdd / ResultsGet`
- Kiến trúc và message flow nhìn chung bám SRS
- Các issue trong audit này đã được sửa trong code và đã verify bằng build solution

## Audit items

### AUD-001

- Status: `FIXED`
- Severity: `HIGH`
- Title: `StudentAdd` đang upsert thay vì insert + reject duplicate
- SRS reference:
  - `docs/SRS.md` FR-S06: insert record, `StudentId` unique
- Current behavior:
  - Server dùng `MERGE` và `WHEN MATCHED THEN UPDATE`, nên cùng `StudentId` sẽ ghi đè dữ liệu cũ.
- Impact:
  - Lệch business rule và acceptance expectation nếu hệ thống phải từ chối bản ghi trùng mã sinh viên.
- Code references:
  - `StudentServer.Console/Data/StudentRepository.cs`
  - `StudentServer.Console/Networking/ClientSession.cs`
- Suggested fix:
  - Đổi sang `INSERT` thuần.
  - Giữ unique constraint.
  - Bắt lỗi duplicate key và trả `StudentAddFail` với message rõ ràng.
- Resolution:
  - Đã đổi từ `MERGE/UPDATE` sang `INSERT`.
  - Đã bắt lỗi SQL duplicate key và trả lỗi rõ ràng cho client.

### AUD-002

- Status: `FIXED`
- Severity: `HIGH`
- Title: Mất kết nối TCP không reset state UI về disconnected
- SRS reference:
  - `docs/SRS.md` 8.3 Reliability: xử lý lỗi TCP, xử lý disconnect
- Current behavior:
  - Khi lỗi mạng, `TcpClientService` đóng socket.
  - Nhưng `IsDbConnected` trên `TcpStudentService` không tự reset theo trạng thái socket.
  - `StudentEntryViewModel` vẫn gate theo `IsDbConnected`, nên nút có thể còn enabled dù TCP đã rơi.
- Impact:
  - UX sai trạng thái, user chỉ biết khi bấm action và nhận lỗi muộn.
- Code references:
  - `StudentClient.Wpf/Services/TcpClientService.cs`
  - `StudentClient.Wpf/Services/TcpStudentService.cs`
  - `StudentClient.Wpf/ViewModels/StudentEntryViewModel.cs`
- Suggested fix:
  - Đồng bộ `IsDbConnected` với trạng thái TCP thực tế.
  - Khi mất socket, reset cờ DB và đẩy UI về trạng thái cần reconnect.
- Resolution:
  - Đã thêm propagation state từ `TcpClientService` lên `TcpStudentService`.
  - Khi socket rơi, state DB được reset và shell tự quay lại màn hình reconnect.
  - Các ViewModel liên quan đã refresh lại trạng thái enable/disable theo state mới.

### AUD-003

- Status: `FIXED`
- Severity: `MEDIUM`
- Title: TCP connect chưa có timeout 3-5 giây như SRS
- SRS reference:
  - `docs/SRS.md` màn hình TCP Connect, behavior: timeout `3-5s`
- Current behavior:
  - Client gọi `TcpClient.ConnectAsync(...)` trực tiếp, chưa có timeout chủ động ở tầng UI/service.
- Impact:
  - Trường hợp server unreachable có thể chờ lâu hơn spec.
- Code references:
  - `StudentClient.Wpf/ViewModels/ConnectionViewModel.cs`
  - `StudentClient.Wpf/Services/TcpClientService.cs`
- Suggested fix:
  - Bọc connect bằng `CancellationTokenSource` hoặc `Task.WhenAny` với timeout rõ ràng.
- Resolution:
  - Đã thêm timeout 5 giây cho TCP connect và hiển thị thông báo timeout rõ ràng cho user.

### AUD-004

- Status: `FIXED`
- Severity: `MEDIUM`
- Title: DB Connect thiếu validation `Password required`
- SRS reference:
  - `docs/SRS.md` validation DB connect: password required
- Current behavior:
  - `CanConnectDb` không check `Password`.
  - `DbConnectRequest.IsValid()` cũng không check `Password`.
- Impact:
  - UI cho submit request không hợp lệ so với spec, lỗi bị đẩy xuống SQL layer.
- Code references:
  - `StudentClient.Wpf/ViewModels/DbConnectViewModel.cs`
  - `Student.Shared/DTOs/DbConnectDtos.cs`
- Suggested fix:
  - Thêm validation password ở cả client VM và shared DTO validation.
- Resolution:
  - Đã thêm validation `Password required` ở cả client ViewModel và shared DTO.

### AUD-005

- Status: `FIXED`
- Severity: `MEDIUM`
- Title: State `Connecting` chưa khóa input đúng như SRS
- SRS reference:
  - `docs/SRS.md` màn hình TCP Connect: `Connecting -> disable input`, `Connected -> khóa input`
  - `docs/SRS.md` màn hình DB Connect: `Connecting -> disable button`
- Current behavior:
  - Button có guard theo `CanConnect` / `CanConnectDb`.
  - Nhưng input fields không bind `IsEnabled` theo `IsBusy`, nên user vẫn sửa field trong lúc request đang chạy.
- Impact:
  - UX không đúng state machine mô tả trong SRS.
- Code references:
  - `StudentClient.Wpf/Views/ConnectionView.xaml`
  - `StudentClient.Wpf/Views/DbConnectView.xaml`
- Suggested fix:
  - Bind `IsEnabled` cho nhóm input theo trạng thái `!IsBusy`.
  - Nếu muốn bám sát SRS hơn, thêm state rõ ràng cho `Connected`.
- Resolution:
  - Đã bind trạng thái enable/disable của input controls theo `IsBusy` và connection state.
  - Các màn hình connect không còn cho sửa input trong lúc request đang chạy.

### AUD-006

- Status: `FIXED`
- Severity: `LOW`
- Title: Một số default/status text chưa khớp mockup SRS
- SRS reference:
  - `docs/SRS.md` default TCP host là `localhost`
  - `docs/SRS.md` status ban đầu là `Disconnected` / `Not Connected`
- Current behavior:
  - TCP host mặc định đang là `127.0.0.1`
  - Status ban đầu là câu hướng dẫn thao tác
- Impact:
  - Sai khác nhỏ về wording và default value, ít ảnh hưởng logic
- Code references:
  - `StudentClient.Wpf/ViewModels/ConnectionViewModel.cs`
  - `StudentClient.Wpf/ViewModels/DbConnectViewModel.cs`
- Suggested fix:
  - Chỉnh default values và status text nếu cần bám spec/mẫu chấm.
- Resolution:
  - Đã đổi TCP host mặc định về `localhost`.
  - Đã chỉnh status ban đầu về `Disconnected` và `Not Connected`.
  - Đã đổi wording nút DB connect thành `Connect Database` để khớp hơn với SRS.

## What already matches SRS

- Có đủ flow chính: `TCP Connect -> DbConnect -> StudentAdd -> ResultsGet`
- Client có hỗ trợ `ResultsGet` với `ALL` và `BY_ID`
- Client hiển thị `FullName`, `StudentId`, `Average`
- Server có DES encrypt/decrypt và tính trung bình sau khi giải mã
- Server xử lý message theo `MessageType`

## Verification note

- Đã review lại source code sau khi fix.
- Đã verify build thành công bằng lệnh `dotnet build 'SocketStudentSystem.sln' -v minimal -clp:ErrorsOnly`.
- Chưa chạy kiểm thử runtime end-to-end với SQL Server thực tế trong turn này.

## Current status

1. `AUD-001`: `FIXED`
2. `AUD-002`: `FIXED`
3. `AUD-003`: `FIXED`
4. `AUD-004`: `FIXED`
5. `AUD-005`: `FIXED`
6. `AUD-006`: `FIXED`
