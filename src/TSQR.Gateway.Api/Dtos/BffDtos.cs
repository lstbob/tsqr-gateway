namespace TSQR.Gateway.Api.Dtos;

public record UserInfo(Guid Id, string Email, string FullName, string Role, Guid? MemberId);

public record DashboardStats(int TotalTools, int TotalMembers, int ActiveLoans, int UnderMaintenance, int PendingReservations);

public record DashboardResponse(UserInfo User, DashboardStats Stats);