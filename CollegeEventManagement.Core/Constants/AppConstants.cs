namespace CollegeEventManagement.Core.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Student = "Student";

    public static readonly string[] All = [Admin, Student];
}

public static class EventCategories
{
    public const string Technical = "Technical";
    public const string Cultural = "Cultural";
    public const string Sports = "Sports";
    public const string Workshop = "Workshop";
    public const string Seminar = "Seminar";

    public static readonly string[] All = [Technical, Cultural, Sports, Workshop, Seminar];
}

public static class RegistrationStatus
{
    public const string Approved = "Approved";
    public const string Pending = "Pending";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Approved, Pending, Rejected, Cancelled];
}

public static class AttendanceStatuses
{
    public const string Unmarked = "Unmarked";
    public const string Present = "Present";
    public const string Absent = "Absent";

    public static readonly string[] All = [Unmarked, Present, Absent];
}
