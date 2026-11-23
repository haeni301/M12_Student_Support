using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StudentSupport.Models;

public static class Roles
{
    public const string Student = "Student";
    public const string Staff = "Staff";
    public const string Manager = "Manager";
    public const string Intake = "Intake";
    public const string Admin = "Admin";
}

public static class TicketStatuses
{
    public const string New = "New";
    public const string Routed = "Routed";
    public const string Assigned = "Assigned";
    public const string InProgress = "InProgress";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";
    public const string Reopened = "Reopened";
}

public static class TicketChoices
{
    public static readonly string[] Categories = ["Academic", "Finance", "Other"];
    public static readonly string[] Priorities = ["Low", "Normal", "High", "Urgent"];
}

public class AppUser
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string UserName { get; set; } = "";
    [Required, MaxLength(120)] public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    [MaxLength(20)] public string Role { get; set; } = Roles.Student;
    public int? DepartmentId { get; set; }
    [MaxLength(20)] public string? StudentCode { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockoutUntilUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StudentProfile
{
    [Key, MaxLength(20)] public string StudentCode { get; set; } = "";
    [Required, MaxLength(120)] public string FullName { get; set; } = "";
    [MaxLength(40)] public string ClassCode { get; set; } = "";
    [MaxLength(120)] public string Program { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class Department
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "";
}

public class SupportTicket
{
    public int Id { get; set; }
    [Required, StringLength(120, MinimumLength = 6)] public string Title { get; set; } = "";
    [Required, StringLength(2000, MinimumLength = 10)] public string Description { get; set; } = "";
    [MaxLength(40)] public string Category { get; set; } = "Academic";
    [MaxLength(20)] public string Priority { get; set; } = "Normal";
    [MaxLength(30)] public string Status { get; set; } = TicketStatuses.New;
    public int StudentId { get; set; }
    [MaxLength(20)] public string? StudentCode { get; set; }
    public int? DepartmentId { get; set; }
    public int? AssignedStaffId { get; set; }
    public bool IsSensitive { get; set; }
    [MaxLength(1500)] public string? ResolutionSummary { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedUtc { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
    public List<TicketComment> Comments { get; set; } = [];
    public List<TicketHistory> History { get; set; } = [];
    public List<TicketAttachment> Attachments { get; set; } = [];
}

public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int AuthorId { get; set; }
    [Required, StringLength(1000)] public string Body { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class TicketHistory
{
    public long Id { get; set; }
    public int TicketId { get; set; }
    public int ActorId { get; set; }
    [Required, MaxLength(32)] public string Event { get; set; } = "";
    [MaxLength(30)] public string? FromStatus { get; set; }
    [MaxLength(30)] public string? ToStatus { get; set; }
    public int? FromDepartmentId { get; set; }
    public int? ToDepartmentId { get; set; }
    public int? FromStaffId { get; set; }
    public int? ToStaffId { get; set; }
    [MaxLength(400)] public string? Note { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class TicketAttachment
{
    public long Id { get; set; }
    public int TicketId { get; set; }
    public int UploadedById { get; set; }
    [Required, MaxLength(180)] public string OriginalName { get; set; } = "";
    [Required, MaxLength(80)] public string StoredName { get; set; } = "";
    [Required, MaxLength(80)] public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class AuditEvent
{
    public long Id { get; set; }
    public int? ActorId { get; set; }
    [MaxLength(60)] public string Action { get; set; } = "";
    public int? TicketId { get; set; }
    public bool Success { get; set; }
    [MaxLength(200)] public string Details { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class LoginInput
{
    [Required] public string Username { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}
public class TicketInput
{
    [Required, StringLength(120, MinimumLength = 6)] public string Title { get; set; } = "";
    [Required, StringLength(2000, MinimumLength = 10)] public string Description { get; set; } = "";
    [Required] public string Category { get; set; } = "Academic";
    public bool IsSensitive { get; set; }
}
public class IntakeInput
{
    [Required] public int TicketId { get; set; }
    [Required] public int DepartmentId { get; set; }
    [Required] public string Category { get; set; } = "Academic";
    [Required] public string Priority { get; set; } = "Normal";
    [StringLength(400)] public string? Note { get; set; }
}
public class StatusInput
{
    public int Id { get; set; }
    [Required] public string Status { get; set; } = "";
    [StringLength(1500)] public string? Resolution { get; set; }
}
public class TransferInput
{
    public int TicketId { get; set; }
    public int DepartmentId { get; set; }
    [Required, StringLength(400, MinimumLength = 5)] public string Reason { get; set; } = "";
}
public class PasswordInput
{
    [Required] public string CurrentPassword { get; set; } = "";
    [Required, MinLength(10)] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = "";
}
public class AttachmentInput
{
    public int TicketId { get; set; }
    [Required] public IFormFile? File { get; set; }
}
public class UserSummary
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public int? DepartmentId { get; set; }
    public string? StudentCode { get; set; }
    public bool IsActive { get; set; }
}
public class DashboardViewModel
{
    public string Role { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int VisibleTotal { get; set; }
    public int NewCount { get; set; }
    public int ActiveCount { get; set; }
    public int DoneCount { get; set; }
    public List<SupportTicket> Recent { get; set; } = [];
}
