using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentSupport.Data;
using StudentSupport.Models;
using StudentSupport.Services;

namespace StudentSupport.Controllers;

[Authorize]
public class TicketsController(AppDbContext db, SecurityService sec, AttachmentService attachments) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        IQueryable<SupportTicket> query = db.Tickets.AsNoTracking();
        query = me.Role switch
        {
            Roles.Student => query.Where(x => x.StudentId == me.Id),
            Roles.Staff => query.Where(x => x.AssignedStaffId == me.Id),
            Roles.Manager => query.Where(x => me.DepartmentId.HasValue && x.DepartmentId == me.DepartmentId),
            Roles.Intake => query.Where(x => x.DepartmentId == null && x.Status == TicketStatuses.New),
            _ => query.Where(x => false)
        };
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            if (q.Length > 100) return BadRequest("Từ khóa quá dài.");
            query = query.Where(t => t.Title.Contains(q));
        }
        return View(await query.OrderByDescending(x => x.CreatedUtc).Take(100).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        var t = await db.Tickets.Include(x => x.Comments).Include(x => x.History)
            .Include(x => x.Attachments).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (!sec.CanRead(me, t))
        {
            await sec.Log(me.Id, "TICKET_ACCESS_DENIED", id, false);
            return Forbid();
        }
        await sec.Log(me.Id, "TICKET_VIEW", id, true);
        ViewBag.Staff = await db.Users.AsNoTracking().Where(u => u.Id == t.AssignedStaffId)
            .Select(x => x.FullName).FirstOrDefaultAsync();
        ViewBag.Department = await db.Departments.AsNoTracking().Where(d => d.Id == t.DepartmentId)
            .Select(x => x.Name).FirstOrDefaultAsync();
        ViewBag.Student = await db.StudentProfiles.AsNoTracking().Where(x => x.StudentCode == t.StudentCode)
            .Select(x => x.FullName).FirstOrDefaultAsync();
        return View(t);
    }

    [Authorize(Roles = Roles.Student), HttpGet]
    public IActionResult Create() => View(new TicketInput());

    [Authorize(Roles = Roles.Student), HttpPost]
    public async Task<IActionResult> Create(TicketInput model)
    {
        if (string.IsNullOrWhiteSpace(model.Title) || model.Title.Trim().Length < 6)
            ModelState.AddModelError(nameof(model.Title), "Tiêu đề phải có ít nhất 6 ký tự thực.");
        if (string.IsNullOrWhiteSpace(model.Description) || model.Description.Trim().Length < 10)
            ModelState.AddModelError(nameof(model.Description), "Nội dung phải có ít nhất 10 ký tự thực.");
        if (!TicketChoices.Categories.Contains(model.Category)) ModelState.AddModelError(nameof(model.Category), "Danh mục không hợp lệ.");
        if (!ModelState.IsValid) return View(model);
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        if (string.IsNullOrWhiteSpace(me.StudentCode) ||
            !await db.StudentProfiles.AnyAsync(x => x.StudentCode == me.StudentCode && x.IsActive))
        {
            ModelState.AddModelError("", "Không tìm thấy hồ sơ sinh viên hợp lệ theo mã chuẩn.");
            return View(model);
        }
        var ticket = new SupportTicket { StudentId = me.Id, StudentCode = me.StudentCode,
            Title = model.Title.Trim(), Description = model.Description.Trim(), Category = model.Category,
            IsSensitive = model.IsSensitive, Priority = "Normal", Status = TicketStatuses.New };
        // Commit ticket + history + audit as one transaction; no orphaned unlogged ticket.
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            db.TicketHistories.Add(SecurityService.History(ticket.Id, me.Id, "CREATED", ticket));
            SecurityService.Record(db, me.Id, ticket.Id, "TICKET_CREATE", true);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        TempData["Success"] = "Đã gửi yêu cầu đến bộ phận tiếp nhận trung tâm.";
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Comment(int id, string body)
    {
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        var t = await db.Tickets.FindAsync(id);
        if (t is null) return NotFound();
        if (!sec.CanRead(me, t)) { await sec.Log(me.Id, "COMMENT_DENIED", id, false); return Forbid(); }
        if (t.Status == TicketStatuses.Closed) return BadRequest("Yêu cầu đã đóng. Hãy mở lại trước khi phản hồi.");
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > 1000) return BadRequest("Nội dung phản hồi phải từ 1 đến 1000 ký tự.");
        db.Comments.Add(new TicketComment { TicketId = id, AuthorId = me.Id, Body = body.Trim() });
        db.TicketHistories.Add(SecurityService.History(id, me.Id, "COMMENT", t));
        SecurityService.Record(db, me.Id, id, "COMMENT_ADD", true);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã gửi phản hồi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = Roles.Staff), HttpPost]
    public async Task<IActionResult> Status(StatusInput model)
    {
        if (!ModelState.IsValid) return BadRequest("Dữ liệu trạng thái không hợp lệ.");
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        var t = await db.Tickets.FindAsync(model.Id);
        if (t is null) return NotFound();
        if (!sec.CanHandle(me, t)) { await sec.Log(me.Id, "STATUS_DENIED", t.Id, false); return Forbid(); }
        var next = model.Status;
        var valid = ((t.Status is TicketStatuses.Assigned or TicketStatuses.Reopened) && next == TicketStatuses.InProgress) ||
                    (t.Status == TicketStatuses.InProgress && next == TicketStatuses.Resolved);
        if (!valid) return BadRequest("Không được bỏ qua các bước trong vòng đời xử lý.");
        if (next == TicketStatuses.Resolved && (string.IsNullOrWhiteSpace(model.Resolution) || model.Resolution.Trim().Length < 10))
            return BadRequest("Phải ghi kết quả giải quyết tối thiểu 10 ký tự.");
        var old = t.Status;
        t.Status = next;
        if (next == TicketStatuses.Resolved) t.ResolutionSummary = model.Resolution?.Trim();
        t.UpdatedUtc = DateTime.UtcNow;
        db.TicketHistories.Add(SecurityService.History(t.Id, me.Id, "STATUS_CHANGE", t, fromStatus: old));
        SecurityService.Record(db, me.Id, t.Id, "STATUS_CHANGE", true, $"{old}->{next}");
        if (!await SaveChangesSafe()) return Conflict("Yêu cầu đã thay đổi; hãy tải lại.");
        TempData["Success"] = "Đã cập nhật trạng thái và lịch sử xử lý.";
        return RedirectToAction(nameof(Details), new { id = t.Id });
    }

    [Authorize(Roles = Roles.Student + "," + Roles.Manager), HttpPost]
    public async Task<IActionResult> Close(int id)
    {
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        var t = await db.Tickets.FindAsync(id);
        if (t is null) return NotFound();
        if (!(me.Role == Roles.Student && t.StudentId == me.Id) &&
            !(me.Role == Roles.Manager && me.DepartmentId.HasValue && t.DepartmentId == me.DepartmentId))
        { await sec.Log(me.Id, "CLOSE_DENIED", id, false); return Forbid(); }
        if (t.Status != TicketStatuses.Resolved) return BadRequest("Chỉ đóng được yêu cầu đã có kết quả xử lý.");
        var old = t.Status;
        t.Status = TicketStatuses.Closed;
        var closedAt = DateTime.UtcNow;
        t.UpdatedUtc = closedAt;
        t.ClosedUtc = closedAt;
        db.TicketHistories.Add(SecurityService.History(id, me.Id, "CLOSED", t, fromStatus: old));
        SecurityService.Record(db, me.Id, id, "TICKET_CLOSED", true);
        if (!await SaveChangesSafe()) return Conflict("Dữ liệu đã thay đổi; hãy tải lại.");
        TempData["Success"] = "Yêu cầu đã được đóng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = Roles.Student), HttpPost]
    public async Task<IActionResult> Reopen(int id, string? reason)
    {
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        var t = await db.Tickets.FindAsync(id);
        if (t is null) return NotFound();
        if (t.StudentId != me.Id) { await sec.Log(me.Id, "REOPEN_DENIED", id, false); return Forbid(); }
        if (t.Status is not (TicketStatuses.Closed or TicketStatuses.Resolved)) return BadRequest("Yêu cầu chưa đủ điều kiện mở lại.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5 || reason.Length > 400)
            return BadRequest("Hãy nêu lý do mở lại (5–400 ký tự).");
        var old = t.Status;
        t.Status = TicketStatuses.Reopened;
        t.ClosedUtc = null;
        t.UpdatedUtc = DateTime.UtcNow;
        db.TicketHistories.Add(SecurityService.History(id, me.Id, "REOPENED", t, fromStatus: old, note: reason.Trim()));
        SecurityService.Record(db, me.Id, id, "TICKET_REOPEN", true);
        if (!await SaveChangesSafe()) return Conflict("Dữ liệu đã thay đổi; hãy tải lại.");
        TempData["Success"] = "Đã mở lại yêu cầu.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(AttachmentInput model)
    {
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        var t = await db.Tickets.FindAsync(model.TicketId);
        if (t is null) return NotFound();
        if (!sec.CanRead(me, t)) { await sec.Log(me.Id, "UPLOAD_DENIED", t.Id, false); return Forbid(); }
        if (t.Status == TicketStatuses.Closed) return BadRequest("Không thêm tệp vào yêu cầu đã đóng.");
        if (model.File is null) return BadRequest("Chưa chọn tệp.");
        TicketAttachment metadata;
        try { metadata = await attachments.SaveAsync(model.File, t.Id, me.Id); }
        catch (ArgumentException ex)
        {
            await sec.Log(me.Id, "UPLOAD_REJECTED", t.Id, false, "Invalid file type/size/content");
            return BadRequest(ex.Message);
        }
        db.TicketAttachments.Add(metadata);
        db.TicketHistories.Add(SecurityService.History(t.Id, me.Id, "ATTACHMENT", t));
        SecurityService.Record(db, me.Id, t.Id, "ATTACHMENT_UPLOAD", true);
        try { await db.SaveChangesAsync(); }
        catch { attachments.DeleteSaved(metadata); throw; }
        TempData["Success"] = "Tệp hợp lệ đã được lưu riêng ngoài thư mục public.";
        return RedirectToAction(nameof(Details), new { id = t.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Download(long id)
    {
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        var attachment = await db.TicketAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (attachment is null) return NotFound();
        var ticket = await db.Tickets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == attachment.TicketId);
        if (ticket is null) return NotFound();
        if (!sec.CanRead(me, ticket))
        { await sec.Log(me.Id, "DOWNLOAD_DENIED", ticket.Id, false); return Forbid(); }
        var path = attachments.Resolve(attachment.StoredName);
        if (!System.IO.File.Exists(path)) return NotFound("Tệp không tồn tại trên bộ nhớ lưu trữ.");
        await sec.Log(me.Id, "ATTACHMENT_DOWNLOAD", ticket.Id, true);
        return PhysicalFile(path, attachment.ContentType, attachment.OriginalName);
    }

    private async Task<bool> SaveChangesSafe()
    {
        try { await db.SaveChangesAsync(); return true; }
        catch (DbUpdateConcurrencyException) { return false; }
    }
}
