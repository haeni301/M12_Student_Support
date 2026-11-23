using Microsoft.EntityFrameworkCore;
using StudentSupport.Models;

namespace StudentSupport.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<SupportTicket> Tickets => Set<SupportTicket>();
    public DbSet<TicketComment> Comments => Set<TicketComment>();
    public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<AuditEvent> Audits => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>().HasIndex(x => x.UserName).IsUnique();
        b.Entity<AppUser>().HasOne<StudentProfile>().WithMany()
            .HasForeignKey(x => x.StudentCode).HasPrincipalKey(x => x.StudentCode)
            .OnDelete(DeleteBehavior.NoAction);
        b.Entity<SupportTicket>().HasIndex(x => new { x.StudentId, x.CreatedUtc });
        b.Entity<SupportTicket>().HasIndex(x => new { x.DepartmentId, x.Status });
        b.Entity<SupportTicket>().HasIndex(x => new { x.Status, x.Priority });
        b.Entity<SupportTicket>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.NoAction);
        b.Entity<SupportTicket>().HasOne<StudentProfile>().WithMany()
            .HasForeignKey(x => x.StudentCode).OnDelete(DeleteBehavior.NoAction);
        b.Entity<SupportTicket>().HasOne<Department>().WithMany()
            .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<SupportTicket>().HasOne<AppUser>().WithMany()
            .HasForeignKey(x => x.AssignedStaffId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<SupportTicket>().Property(x => x.RowVersion).IsRowVersion();
        b.Entity<TicketComment>().HasOne<SupportTicket>().WithMany(x => x.Comments)
            .HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TicketComment>().HasOne<AppUser>().WithMany()
            .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TicketHistory>().HasOne<SupportTicket>().WithMany(x => x.History)
            .HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TicketHistory>().HasOne<AppUser>().WithMany()
            .HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TicketAttachment>().HasOne<SupportTicket>().WithMany(x => x.Attachments)
            .HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TicketAttachment>().HasOne<AppUser>().WithMany()
            .HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.NoAction);
    }
}
