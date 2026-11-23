using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentSupport.Data;
using StudentSupport.Models;
using StudentSupport.Services;

namespace StudentSupport.Controllers;
[Authorize]
public class DemoController(AppDbContext db, SecurityService sec) : Controller
{
    public async Task<IActionResult> Index()
    {
        var me = await sec.Current(User);
        if (me is null) return Forbid();
        IQueryable<SupportTicket> scoped = db.Tickets.AsNoTracking();
        scoped = me.Role switch
        {
            Roles.Student => scoped.Where(x => x.StudentId == me.Id),
            Roles.Staff => scoped.Where(x => x.AssignedStaffId == me.Id),
            Roles.Manager => scoped.Where(x => me.DepartmentId.HasValue && x.DepartmentId == me.DepartmentId),
            Roles.Intake => scoped.Where(x => x.DepartmentId == null && x.Status == TicketStatuses.New),
            _ => scoped.Where(x => false)
        };
        var dashboard = new DashboardViewModel
        {
            Role = me.Role, DisplayName = me.FullName,
            VisibleTotal = await scoped.CountAsync(),
            NewCount = await scoped.CountAsync(x => x.Status == TicketStatuses.New || x.Status == TicketStatuses.Routed),
            ActiveCount = await scoped.CountAsync(x => x.Status == TicketStatuses.Assigned ||
                x.Status == TicketStatuses.InProgress || x.Status == TicketStatuses.Reopened),
            DoneCount = await scoped.CountAsync(x => x.Status == TicketStatuses.Resolved || x.Status == TicketStatuses.Closed),
            Recent = await scoped.OrderByDescending(x => x.UpdatedUtc).Take(5).ToListAsync()
        };
        return View(dashboard);
    }
}
