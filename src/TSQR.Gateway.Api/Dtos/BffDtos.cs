namespace TSQR.Gateway.Api.Dtos;

public record UserInfo(Guid Id, string Email, string FullName, string Role, Guid? MemberId);

public record DashboardStats(int TotalTools, int TotalMembers, int ActiveLoans, int UnderMaintenance, int PendingReservations);

public record SoupKitchenDashboardStats(int TotalEvents, int UpcomingEvents, int TotalVolunteers, int TotalDonations, int TotalGuests);

public record DashboardResponse(UserInfo User, DashboardStats Stats, SoupKitchenDashboardStats SoupKitchenStats);
