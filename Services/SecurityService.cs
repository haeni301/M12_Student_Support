using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StudentSupport.Data;
using StudentSupport.Models;

namespace StudentSupport.Services;
public class SecurityService(AppDbContext db)
{
    public static int? Id(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public async Task<AppUser?> Current(ClaimsPrincipal user)
    {
        var id = Id(user);
        if (id is null) return null;
        var record = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive);
        // Refuse stale/tampered role claims even before cookie principal revalidation.
        return record is not null && record.Role == user.FindFirstValue(ClaimTypes.Role) ? record : null;
    }

    public bool CanRead(AppUser me, SupportTicket ticket) => me.Role switch
    {
        Roles.Student => ticket.StudentId == me.Id,
        Roles.Staff => ticket.AssignedStaffId == me.Id,
        Roles.Manager => me.DepartmentId.HasValue && ticket.DepartmentId == me.DepartmentId,
        Roles.Intake => ticket.Status == TicketStatuses.New && ticket.DepartmentId is null,
        _ => false // Admin may inspect security metadata but NOT private request content
    };

    public bool CanHandle(AppUser me, SupportTicket ticket) =>
        (me.Role == Roles.Staff && ticket.AssignedStaffId == me.Id) ||
        (me.Role == Roles.Manager && me.DepartmentId.HasValue && ticket.DepartmentId == me.DepartmentId);

    public bool CanTransfer(AppUser me, SupportTicket ticket) =>
        me.Role == Roles.Manager && me.DepartmentId.HasValue && ticket.DepartmentId == me.DepartmentId &&
        ticket.Status != TicketStatuses.Closed && ticket.Status != TicketStatuses.Resolved;

    public Task Log(int? actor, string action, int? ticket, bool ok, string detail = "")
    {
        db.Audits.Add(new AuditEvent { ActorId = actor, Action = action, TicketId = ticket,
            Success = ok, Details = detail.Length > 200 ? detail[..200] : detail });
        return db.SaveChangesAsync();
    }

    public static void Record(AppDbContext db, int actor, int ticketId, string action, bool allowed, string detail = "") =>
        db.Audits.Add(new AuditEvent { ActorId = actor, TicketId = ticketId, Action = action,
            Success = allowed, Details = detail.Length > 200 ? detail[..200] : detail });

    public static TicketHistory History(int ticket, int actor, string type, SupportTicket before,
        string? fromStatus = null, int? fromDepartment = null, int? fromStaff = null, string? note = null) =>
        new() { TicketId = ticket, ActorId = actor, Event = type,
            FromStatus = fromStatus, ToStatus = before.Status,
            FromDepartmentId = fromDepartment, ToDepartmentId = before.DepartmentId,
            FromStaffId = fromStaff, ToStaffId = before.AssignedStaffId,
            Note = note is null ? null : note.Length > 400 ? note[..400] : note };
}
