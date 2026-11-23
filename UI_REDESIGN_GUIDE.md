# M12 v2 – Cập nhật giao diện cổng hỗ trợ người học

## Những thay đổi

- Tham khảo bố cục cổng thông tin tại https://neu.edu.vn/lien-he/ (lấy cảm hứng về cấu trúc và phong cách, **không sao chép** logo hay giao diện chính thức).
- Dùng header tổ chức, thanh điều hướng ngang theo quyền, breadcrumb, footer, màu xanh–trắng và điểm nhấn vàng.
- Ưu tiên font Be Vietnam Pro từ Google Fonts, với Segoe UI và Arial dự phòng khi không có mạng.
- Bỏ emoji khỏi các trang Razor, bỏ hoàn toàn hai khối “Demo độc đáo” và “4 màn demo khiến phần thuyết trình dễ nhớ”.
- Trang Tổng quan chuyển thành dashboard dịch vụ người học: KPI thực, yêu cầu gần đây và quy trình hỗ trợ gọn gàng.
- Trang ticket, phân loại, phân công, nhật ký, login và quản lý tài khoản được đồng nhất kiểu chữ, form, bảng và màu trạng thái.
- Nhãn trạng thái, mức ưu tiên, vai trò và phân loại được hiển thị tiếng Việt qua `Models/UiLabels.cs`; **giá trị dữ liệu SQL không thay đổi**.

## Cách cập nhật trên Windows

**Cách khuyên dùng:**
1. Dừng website đang chạy (Ctrl+C).
2. Sao chép dự án đang chạy sang một thư mục backup nếu có tùy chỉnh riêng.
3. Giải nén ZIP mới vào một thư mục khác, ví dụ `D:\Security\M12_StudentSupport_v2_NEU_UI`.
4. Mở PowerShell tại thư mục `M12_StudentSupport` chứa `StudentSupport.csproj`.
5. Chạy:

```powershell
powershell -ExecutionPolicy Bypass -File .\RUN_M12_DEMO_SQLEXPRESS.ps1
```

6. Truy cập `https://localhost:50900/` và nhấn **Ctrl+F5** để bỏ cache CSS cũ.

**Cách chỉ thay đổi UI trên dự án đang chạy:** chép đè thư mục `Views`, file `wwwroot/css/site.css` và thêm `Models/UiLabels.cs`. Không cần chạy SQL script, không cần xóa database. Nên backup trước khi chép đè để giữ phần chỉnh sửa riêng.

## Những chức năng giữ nguyên

- Các Controller, `Program.cs`, EF Core context, entity nghiệp vụ, phân quyền và nghiệp vụ cập nhật/tải tệp không bị sửa.
- Login, tiếp nhận, phân công, chuyển bộ phận, xử lý trạng thái, phản hồi, tải tệp, audit vẫn giữ đường dẫn/action cũ.
- `RUN_M12_DEMO_SQLEXPRESS.ps1` vẫn sử dụng database `M12_StudentSupport_V2_Demo_20261118` trên `.\SQLEXPRESS`. Không cần tạo database hoặc chạy migration lại chỉ vì đổi giao diện.
- Demo bảo mật vẫn thực hiện được trực tiếp trong các chức năng thật; hướng dẫn demo nằm trong `DOCS_PLAN.md` và bộ `tests/TEST_CASES.md`, không hiển thị thành khối quảng bá trên màn hình.

## Kiểm tra lại sau khi chạy

1. Đăng nhập `sv001` → Tổng quan → Danh sách yêu cầu → Gửi yêu cầu.
2. Đăng nhập `intake` → Tiếp nhận và phân loại.
3. Đăng nhập `managerdt` → Phân công và chuyển bộ phận.
4. Đăng nhập `staffdt` → Mở chi tiết, xử lý, đính kèm và phản hồi.
5. Đăng nhập `admin` → Nhật ký bảo mật và Quản lý tài khoản.
6. Thử thu nhỏ cửa sổ/trên di động: menu ngang cuộn được, KPI chuyển thành hai cột.

## Lưu ý

- Logo tròn `M12` là dấu hiệu nhận diện **dự án thực hành**, không phải logo NEU.
- Bản xem trước `docs/UI_PREVIEW.html` là bản trình bày tĩnh bằng dữ liệu minh họa, không phải website đang kết nối database.
- Phiên bản này được kiểm tra nguồn tĩnh; cần xác nhận `dotnet build` và giao diện thực tế trên máy Windows của bạn.
