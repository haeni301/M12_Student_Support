using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentSupport.Data;
using StudentSupport.Models;
using StudentSupport.Services;

namespace StudentSupport.Controllers;
[Authorize(Roles = Roles.Intake)]
public class IntakeController(AppDbContext db, SecurityService sec) : Controller
{
    public async Task<IActionResult> Queue()
    {
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        ViewBag.Departments = await db.Departments.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        return View(await db.Tickets.AsNoTracking()
            .Where(x => x.DepartmentId == null && x.Status == TicketStatuses.New)
            .OrderByDescending(x => x.CreatedUtc).Take(100).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Triage(IntakeInput model)
    {
        if (!ModelState.IsValid || !TicketChoices.Categories.Contains(model.Category) ||
            !TicketChoices.Priorities.Contains(model.Priority)) return BadRequest("Thông tin phân loại không hợp lệ.");
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        var ticket = await db.Tickets.FindAsync(model.TicketId);
        if (ticket is null) return NotFound();
        if (!sec.CanRead(me, ticket))
        { await sec.Log(me.Id, "TRIAGE_DENIED", model.TicketId, false); return Forbid(); }
        if (!await db.Departments.AnyAsync(d => d.Id == model.DepartmentId)) return BadRequest("Đơn vị xử lý không tồn tại.");
        var oldStatus = ticket.Status;
        var oldDepartment = ticket.DepartmentId;
        ticket.Category = model.Category;
        ticket.Priority = model.Priority;
        ticket.DepartmentId = model.DepartmentId;
        ticket.AssignedStaffId = null;
        ticket.Status = TicketStatuses.Routed;
        ticket.UpdatedUtc = DateTime.UtcNow;
        db.TicketHistories.Add(SecurityService.History(ticket.Id, me.Id, "TRIAGED", ticket,
            fromStatus: oldStatus, fromDepartment: oldDepartment, note: model.Note));
        SecurityService.Record(db, me.Id, ticket.Id, "TICKET_TRIAGE", true,
            $"DepartmentId={model.DepartmentId};Category={model.Category};Priority={model.Priority}");
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("Ticket đã được xử lý ở một phiên khác."); }
        TempData["Success"] = "Đã phân loại và chuyển yêu cầu đến hàng đợi bộ phận.";
        return RedirectToAction(nameof(Queue));
    }
}
