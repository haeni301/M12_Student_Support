namespace StudentSupport.Models;

/// <summary>
/// Presentation-only Vietnamese labels. Stored values remain unchanged so existing
/// tickets, database schema and authorization policies keep working.
/// </summary>
public static class UiLabels
{
    public static string Status(string? code) => code switch
    {
        TicketStatuses.New => "Mới tiếp nhận",
        TicketStatuses.Routed => "Chờ phân công",
        TicketStatuses.Assigned => "Đã phân công",
        TicketStatuses.InProgress => "Đang xử lý",
        TicketStatuses.Resolved => "Đã có kết quả",
        TicketStatuses.Closed => "Đã đóng",
        TicketStatuses.Reopened => "Đã mở lại",
        _ => code ?? "Chưa xác định"
    };

    public static string Category(string? code) => code switch
    {
        "Academic" => "Học tập",
        "Finance" => "Tài chính",
        "Other" => "Khác",
        _ => code ?? "Chưa phân loại"
    };

    public static string Priority(string? code) => code switch
    {
        "Low" => "Thấp",
        "Normal" => "Bình thường",
        "High" => "Cao",
        "Urgent" => "Khẩn cấp",
        _ => code ?? "Chưa xác định"
    };

    public static string Role(string? code) => code switch
    {
        Roles.Student => "Sinh viên",
        Roles.Staff => "Nhân viên",
        Roles.Manager => "Quản lý bộ phận",
        Roles.Intake => "Tiếp nhận",
        Roles.Admin => "Quản trị hệ thống",
        _ => code ?? "Khách"
    };
}
