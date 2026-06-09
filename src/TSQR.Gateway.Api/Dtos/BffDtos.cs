namespace TSQR.Gateway.Api.Dtos;

public record UserInfo(string Id, string? Email, string? FirstName, string? LastName, string? UserName, IList<string> Roles, bool IsLockedOut, DateTime CreatedAt);

public record DashboardStats(int TotalTools, int TotalMembers, int ActiveLoans, int UnderMaintenance, int PendingReservations);

public record DashboardResponse(UserInfo User, DashboardStats Stats);
