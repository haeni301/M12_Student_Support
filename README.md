<<<<<<< HEAD
# M12_StudentSupport
Môn Bảo mật phần mềm và cơ sở dữ liệu
=======
# M12 Student Support v2 — Secure Workflow Showcase

**Mục tiêu:** Bài thực hành giữa kỳ môn Bảo mật cơ sở dữ liệu, Module 12 – Hỗ trợ người học. Website ASP.NET Core MVC (.NET 10) + EF Core + SQL Server; RBAC, hồ sơ sinh viên chuẩn, full workflow, secure upload, security audit dashboard.

> **MỚI:** Xem `DOCS_PLAN.md` để có hướng dẫn từng bước từ cài đặt, nâng cấp SQL cũ, test 25 ca, thuyết trình 8–10 phút và cách chụp minh chứng.

## Chạy nhanh với database demo MỚI (chưa có M12_StudentSupport)
1. Cài .NET 10 SDK, SQL Server, SSMS; kiểm tra `dotnet --version`.
2. Điều chỉnh `ConnectionStrings:Default` trong `appsettings.json` (mặc định Windows Auth + SQL Server localhost); nếu dùng LocalDB, sửa thành `Server=(localdb)\\MSSQLLocalDB;Database=M12_StudentSupport;Trusted_Connection=True;TrustServerCertificate=True;` trong JSON.
3. `dotnet dev-certs https --trust`.
4. `dotnet restore` và `dotnet build`.
5. `dotnet run --launch-profile M12-HTTPS-Demo` → truy cập **https://localhost:50900**.
6. Seed chỉ chạy khi `ASPNETCORE_ENVIRONMENT=Development` và `DEMO_SEED=true`.

## Đã có database M12 v1 trên SQL Server?
- Backup database. **Dừng app**. Chạy `sql/upgrade_v1_to_v2.sql` trên đúng database trong SSMS.
- Khởi động lại với profile `M12-HTTPS-Demo` để bổ sung dữ liệu mô phỏng v2 còn thiếu. Không xóa dữ liệu cũ.
- **Không** dùng `EnsureCreated` thay EF Migration cho production. Bản lab dùng schema v2 mới hoặc SQL upgrade script khi đã có v1.

## Tài khoản demo
Mật khẩu ban đầu ở Development: `Demo@12345` (đổi nếu demo dài hạn, không dùng production).

| Username | Role | Phạm vi |
|---|---|---|
| sv001, sv002 | Student | Ticket do chính mình tạo |
| intake | Intake | Ticket mới chưa phân bộ phận |
| managerdt / staffdt | Manager / Staff | Phòng Đào tạo |
| managerct / staffct | Manager / Staff | Phòng Công tác SV |
| managertc / stafftc | Manager / Staff | Phòng Tài chính |
| admin | Admin | Audit và metadata tài khoản, không đọc ticket |

## Tính năng mới
- Intake Queue: nhận ticket New và phân loại → Routed.
- Manager Queue: assign đúng người đúng đơn vị, chuyển bộ phận kèm lý do, thu hồi quyền bộ phận cũ.
- Staff workflow: Assigned → InProgress → Resolved (bắt buộc kết quả).
- Student: đóng/mở lại, phản hồi, đính kèm, chỉ xem ticket của mình.
- Hồ sơ sinh viên `StudentProfiles` có khóa chuẩn `StudentCode` và liên kết FK.
- Lịch sử nghiệp vụ `TicketHistories` (timeline), bảng `Audits` truy vết bảo mật.
- Secure File Vault: tối đa 5 MB; PDF/PNG/JPEG/TXT; kiểm tra magic bytes/UTF-8; lưu `App_Data/attachments` ngoài wwwroot; authorize download.
- Dashboard trực quan theo role và Security Audit Center (dữ liệu log thật, không giả lập sự kiện).
- Cookie Secure/HttpOnly/SameSite; kiểm tra lại role từ DB mỗi request; form anti-forgery; Rate limiter cho login, lockout; password hashing.

## Giới hạn trung thực
- Môi trường tạo gói không có .NET SDK và SQL Server; **chưa build/run integration test ở Windows**. Làm `dotnet restore`, `dotnet build`, `dotnet run`, kiểm thử theo DOCS_PLAN trước khi nộp.
- Chưa có malware scanner, MIME sniffing chuyên sâu, file encryption at rest, HA, CI/CD production.
- Application Audit không thay SQL Server Audit độc lập; dùng `sql/sql_server_audit_optional.sql` nếu yêu cầu.
- RBAC dữ liệu theo hàng do ứng dụng kiểm soát; **không được gọi là SQL Server RLS**. SQL user chung có quyền đọc dữ liệu cần cho đăng nhập.
- Seed ví dụ chỉ trong Development, không nhập PII thật.
>>>>>>> c5785a7 (first commit)
