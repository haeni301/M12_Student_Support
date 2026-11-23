using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentSupport.Data;
using StudentSupport.Models;
using StudentSupport.Services;

namespace StudentSupport.Controllers;
[Authorize]
public class AdminController(AppDbContext db, SecurityService sec) : Controller
{
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Audit(string? action)
    {
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        var query = db.Audits.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(action))
        {
            if (action.Length > 60) return BadRequest();
            query = query.Where(x => x.Action == action);
        }
        ViewBag.Allowed = await db.Audits.CountAsync(x => x.Success);
        ViewBag.Denied = await db.Audits.CountAsync(x => !x.Success);
        return View(await query.OrderByDescending(x => x.Id).Take(200).ToListAsync());
    }

    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Queue()
    {
        var me = await sec.Current(User);
        if (me is null || !me.DepartmentId.HasValue) return Forbid();
        ViewBag.Staff = await db.Users.AsNoTracking().Where(x => x.IsActive && x.Role == Roles.Staff && x.DepartmentId == me.DepartmentId)
            .OrderBy(x => x.UserName).ToListAsync();
        ViewBag.Departments = await db.Departments.AsNoTracking().Where(x => x.Id != me.DepartmentId)
            .OrderBy(x => x.Name).ToListAsync();
        return View(await db.Tickets.AsNoTracking().Where(x => x.DepartmentId == me.DepartmentId)
            .OrderByDescending(x => x.CreatedUtc).Take(100).ToListAsync());
    }

    [Authorize(Roles = Roles.Manager), HttpPost]
    public async Task<IActionResult> Assign(int ticketId, int staffId)
    {
        var me = await sec.Current(User);
        if (me is null || !me.DepartmentId.HasValue) return Forbid();
        var ticket = await db.Tickets.FindAsync(ticketId);
        if (ticket is null) return NotFound();
        if (ticket.DepartmentId != me.DepartmentId)
        { await sec.Log(me.Id, "ASSIGN_DENIED", ticketId, false); return Forbid(); }
        if (ticket.Status is not (TicketStatuses.Routed or TicketStatuses.Assigned or TicketStatuses.Reopened))
            return BadRequest("Chỉ phân công yêu cầu đã chuyển bộ phận hoặc được mở lại.");
        var staff = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == staffId && x.IsActive &&
            x.Role == Roles.Staff && x.DepartmentId == me.DepartmentId);
        if (staff is null) return BadRequest("Nhân viên không thuộc đơn vị quản lý.");
        var oldStatus = ticket.Status;
        var oldStaff = ticket.AssignedStaffId;
        ticket.AssignedStaffId = staff.Id;
        ticket.Status = TicketStatuses.Assigned;
        ticket.UpdatedUtc = DateTime.UtcNow;
        db.TicketHistories.Add(SecurityService.History(ticket.Id, me.Id, "ASSIGNED", ticket,
            fromStatus: oldStatus, fromStaff: oldStaff));
        SecurityService.Record(db, me.Id, ticket.Id, "TICKET_ASSIGN", true, $"StaffId={staff.Id}");
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("Ticket đã thay đổi; hãy tải lại."); }
        TempData["Success"] = "Đã giao ticket cho nhân viên hợp lệ.";
        return RedirectToAction(nameof(Queue));
    }

    [Authorize(Roles = Roles.Manager), HttpPost]
    public async Task<IActionResult> Transfer(TransferInput model)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Reason) || model.Reason.Trim().Length < 5) return BadRequest("Vui lòng nhập lý do chuyển bộ phận từ 5–400 ký tự.");
        var me = await sec.Current(User);
        if (me is null || !me.DepartmentId.HasValue) return Forbid();
        var ticket = await db.Tickets.FindAsync(model.TicketId);
        if (ticket is null) return NotFound();
        if (!sec.CanTransfer(me, ticket))
        { await sec.Log(me.Id, "TRANSFER_DENIED", model.TicketId, false); return Forbid(); }
        if (model.DepartmentId == me.DepartmentId ||
            !await db.Departments.AnyAsync(x => x.Id == model.DepartmentId))
            return BadRequest("Phải chọn bộ phận đích khác và hợp lệ.");
        var oldStatus = ticket.Status;
        var oldDepartment = ticket.DepartmentId;
        var oldStaff = ticket.AssignedStaffId;
        ticket.DepartmentId = model.DepartmentId;
        ticket.AssignedStaffId = null;
        ticket.Status = TicketStatuses.Routed;
        ticket.UpdatedUtc = DateTime.UtcNow;
        db.TicketHistories.Add(SecurityService.History(ticket.Id, me.Id, "TRANSFERRED", ticket,
            fromStatus: oldStatus, fromDepartment: oldDepartment, fromStaff: oldStaff, note: model.Reason.Trim()));
        SecurityService.Record(db, me.Id, ticket.Id, "TICKET_TRANSFER", true,
            $"DepartmentId={oldDepartment}->{model.DepartmentId}");
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("Ticket đã được thay đổi; hãy tải lại."); }
        TempData["Success"] = "Đã chuyển bộ phận. Bộ phận cũ mất quyền truy cập; bộ phận mới thấy yêu cầu.";
        return RedirectToAction(nameof(Queue));
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Users()
    {
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        var list = await db.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new UserSummary
        {
            Id = x.Id, UserName = x.UserName, FullName = x.FullName,
            Role = x.Role, DepartmentId = x.DepartmentId, StudentCode = x.StudentCode, IsActive = x.IsActive
        }).ToListAsync();
        return View(list);
    }
}
