# Bộ test thực hành M12 v2 – ghi kết quả thật sau khi chạy

**Quy ước:** `Expected` là kỳ vọng theo mã nguồn, không phải kiểm thử runtime đã xác nhận. Điền PASS/FAIL, ảnh màn hình và ghi chú. Luôn dùng dữ liệu GIẢ LẬP.

| ID | Loại | Người dùng / thao tác | Expected | Thực tế | Ảnh minh chứng |
|---|---|---|---|---|---|
| TC01 | Auth | sv001 đăng nhập đúng | Vào dashboard Student | Chưa chạy | |
| TC02 | Auth | sv001 nhập sai mật khẩu 5 lần | Lock 5 phút; có LOGIN_FAILED / BLOCKED | Chưa chạy | |
| TC03 | Auth | Đăng nhập rồi đổi mật khẩu (đủ mạnh) | Đổi hash, logout, đăng nhập lại bằng mật khẩu mới | Chưa chạy | |
| TC04 | RBAC | sv001 mở ticket của sv002 qua /Tickets/Details/{id} | 403; TICKET_ACCESS_DENIED | Chưa chạy | |
| TC05 | RBAC | staffdt mở ticket được giao | 200; thấy nội dung | Chưa chạy | |
| TC06 | RBAC | staffdt mở ticket staffct được giao | 403 | Chưa chạy | |
| TC07 | RBAC | admin mở ticket bằng ID hợp lệ | 403; vẫn xem được /Admin/Audit | Chưa chạy | |
| TC08 | Intake | sv001 tạo ticket Finance | New; DepartmentId NULL; intake thấy ngay | Chưa chạy | |
| TC09 | Intake | intake phân loại Finance, Priority High, chọn Phòng Tài chính | Ticket Routed; có history TRIAGED | Chưa chạy | |
| TC10 | Manager | managertc mở Queue | Có ticket Finance sau triage | Chưa chạy | |
| TC11 | Manager | managertc giao stafftc | Assigned; StaffId stafftc; history ASSIGNED | Chưa chạy | |
| TC12 | Manager | managerdt cố phân công ticket Phòng Tài chính | 403/không được truy cập | Chưa chạy | |
| TC13 | Workflow | stafftc nhấn Bắt đầu xử lý | InProgress; history STATUS_CHANGE | Chưa chạy | |
| TC14 | Workflow | stafftc hoàn tất mà bỏ trống kết quả | 400; trạng thái không đổi | Chưa chạy | |
| TC15 | Workflow | stafftc hoàn tất với kết quả >=10 ký tự | Resolved, lưu ResolutionSummary | Chưa chạy | |
| TC16 | Workflow | sv001 xác nhận đóng | Closed, ClosedUtc ghi nhận | Chưa chạy | |
| TC17 | Workflow | sv001 mở lại với lý do hợp lệ | Reopened, history REOPENED | Chưa chạy | |
| TC18 | Transfer | managerdt chuyển ticket đúng quyền sang Phòng Công tác SV | Routed, AssignedStaffId NULL, history TRANSFERRED | Chưa chạy | |
| TC19 | Transfer/RBAC | managerdt mở ticket ngay sau khi chuyển | 403 (quyền bị thu hồi) | Chưa chạy | |
| TC20 | Transfer/RBAC | managerct mở ticket đã nhận và giao staffct | 200, Queue có ticket | Chưa chạy | |
| TC21 | Upload | sv001 tải PDF thật <=5 MB | Có metadata, tệp ngoài wwwroot | Chưa chạy | |
| TC22 | Upload | sv001 tải file .exe hoặc file txt đổi tên .pdf | 400, log UPLOAD_REJECTED | Chưa chạy | |
| TC23 | Download/RBAC | sv002 tải AttachmentId của sv001 | 403, log DOWNLOAD_DENIED | Chưa chạy | |
| TC24 | XSS | Nhập `<script>alert(1)</script>` vào comment | Chỉ hiển thị text, không chạy | Chưa chạy | |
| TC25 | SQLi | Tìm `' OR 1=1 --` ở trang Tickets | Không mở rộng phạm vi bản ghi | Chưa chạy | |
| TC26 | CSRF | POST có cookie nhưng không antiforgery token | 400; không ghi dữ liệu | Chưa chạy | |
| TC27 | SQL Privilege | EXECUTE AS m12_app_user; DELETE Tickets | SQL DENY; không xóa | Chưa chạy | |
| TC28 | SQL Privilege | EXECUTE AS m12_app_user; UPDATE Users.Role | SQL DENY; Role giữ nguyên | Chưa chạy | |
| TC29 | Privacy | Inspect HTML/Network của sv001 | Không thấy PasswordHash / ticket sv002 | Chưa chạy | |
| TC30 | Audit | admin xem màn hình Audit | Thấy ALLOW/DENY của các ca đã chạy | Chưa chạy | |

## Bằng chứng bắt buộc
- Mỗi testcase: ảnh người thực hiện + URL + kết quả; lỗi kiểm thử có ảnh hoặc log.
- Chụp SSMS: cấu trúc bảng, dữ liệu StudentProfiles–Tickets–Histories–Audits, quyền login.
- Chụp 403 và cùng hành động `TICKET_ACCESS_DENIED` tại Audit Center.
- Chụp tệp bị từ chối; tệp hợp lệ ở thư mục App_Data, không có URL public.
- Ghi số test PASS/FAIL thực tế, không tự nhận đạt 30/30 khi chưa chạy.
