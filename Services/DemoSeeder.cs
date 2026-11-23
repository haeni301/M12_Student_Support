using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentSupport.Data;
using StudentSupport.Models;

namespace StudentSupport.Services;

public static class DemoSeeder
{
    public static async Task Seed(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        var hasher = services.GetRequiredService<IPasswordHasher<AppUser>>();

        var people = new[] {
            new StudentProfile { StudentCode = "SV001", FullName = "Nguyễn Minh An", ClassCode = "CNTT-K66", Program = "Công nghệ thông tin" },
            new StudentProfile { StudentCode = "SV002", FullName = "Trần Thảo Linh", ClassCode = "CNTT-K66", Program = "Công nghệ thông tin" }
        };
        foreach (var p in people)
            if (!await db.StudentProfiles.AnyAsync(x => x.StudentCode == p.StudentCode)) db.StudentProfiles.Add(p);
        await db.SaveChangesAsync();

        var departmentNames = new[] { "Phòng Đào tạo", "Phòng Công tác SV", "Phòng Tài chính" };
        foreach (var name in departmentNames)
            if (!await db.Departments.AnyAsync(x => x.Name == name)) db.Departments.Add(new Department { Name = name });
        await db.SaveChangesAsync();
        var departments = await db.Departments.ToDictionaryAsync(x => x.Name, x => x.Id);
        var training = departments["Phòng Đào tạo"];
        var affairs = departments["Phòng Công tác SV"];
        var finance = departments["Phòng Tài chính"];

        var users = new (string user, string role, int? department, string? studentCode)[] {
            ("sv001", Roles.Student, null, "SV001"), ("sv002", Roles.Student, null, "SV002"),
            ("intake", Roles.Intake, null, null),
            ("staffdt", Roles.Staff, training, null), ("managerdt", Roles.Manager, training, null),
            ("staffct", Roles.Staff, affairs, null), ("managerct", Roles.Manager, affairs, null),
            ("stafftc", Roles.Staff, finance, null), ("managertc", Roles.Manager, finance, null),
            ("admin", Roles.Admin, null, null)
        };
        foreach (var item in users)
        {
            if (await db.Users.AnyAsync(x => x.UserName == item.user)) continue;
            var u = new AppUser { UserName = item.user, FullName = item.user.ToUpperInvariant(),
                Role = item.role, DepartmentId = item.department, StudentCode = item.studentCode };
            u.PasswordHash = hasher.HashPassword(u, "Demo@12345");
            db.Users.Add(u);
        }
        await db.SaveChangesAsync();
        var byName = await db.Users.ToDictionaryAsync(x => x.UserName, x => x.Id);

        var examples = new[] {
            new SupportTicket { Title = "Xin xác nhận kết quả học phần", Description = "Nhờ kiểm tra kết quả học phần lập trình và hướng dẫn thủ tục phúc khảo.",
                StudentId = byName["sv001"], StudentCode = "SV001", Category = "Academic", DepartmentId = training,
                AssignedStaffId = byName["staffdt"], Status = TicketStatuses.InProgress, Priority = "High" },
            new SupportTicket { Title = "Yêu cầu hỗ trợ học phí", Description = "Cần kiểm tra số tiền đã đóng và trạng thái biên lai.",
                StudentId = byName["sv001"], StudentCode = "SV001", Category = "Finance", Status = TicketStatuses.New, Priority = "Normal" },
            new SupportTicket { Title = "Tư vấn học bổng sinh viên", Description = "Em muốn biết hồ sơ cần chuẩn bị để nộp học bổng học kỳ tới.",
                StudentId = byName["sv002"], StudentCode = "SV002", Category = "Other", DepartmentId = affairs,
                Status = TicketStatuses.Routed },
            new SupportTicket { Title = "Tra cứu hồ sơ sinh viên riêng tư", Description = "Dữ liệu hoàn toàn giả lập phục vụ bài kiểm thử access control.",
                StudentId = byName["sv002"], StudentCode = "SV002", Category = "Other", DepartmentId = affairs,
                AssignedStaffId = byName["staffct"], Status = TicketStatuses.Assigned, IsSensitive = true },
            new SupportTicket { Title = "Đã hoàn tất hướng dẫn đăng ký môn", Description = "Nhờ tư vấn thủ tục đăng ký học phần tự chọn.",
                StudentId = byName["sv001"], StudentCode = "SV001", Category = "Academic", DepartmentId = training,
                AssignedStaffId = byName["staffdt"], Status = TicketStatuses.Resolved,
                ResolutionSummary = "Đã gửi hướng dẫn đăng ký và xác nhận sinh viên đã nhận được." }
        };
        foreach (var ticket in examples)
        {
            if (await db.Tickets.AnyAsync(x => x.Title == ticket.Title && x.StudentCode == ticket.StudentCode)) continue;
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            db.TicketHistories.Add(new TicketHistory { TicketId = ticket.Id, ActorId = ticket.StudentId,
                Event = "DEMO_SEED", ToStatus = ticket.Status, ToDepartmentId = ticket.DepartmentId,
                ToStaffId = ticket.AssignedStaffId, Note = "Kịch bản minh họa – dữ liệu giả lập" });
        }
        await db.SaveChangesAsync();
    }
}
