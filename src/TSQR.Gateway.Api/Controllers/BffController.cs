using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSQR.Gateway.Api.Dtos;

namespace TSQR.Gateway.Api.Controllers;

[ApiController]
[Route("api/bff")]
[Authorize]
public sealed class BffController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    private const string AutheoClient = "autheo";
    private const string ToolLibClient = "tool-lib";
    private const string SoupKitchenClient = "soup-kitchen";
    private const string CommunitiesClient = "communities";

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardResponse>> Dashboard(CancellationToken ct)
    {
        var token = HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        var autheoClient = httpClientFactory.CreateClient(AutheoClient);
        var toolLibClient = httpClientFactory.CreateClient(ToolLibClient);
        var soupKitchenClient = httpClientFactory.CreateClient(SoupKitchenClient);
        var communitiesClient = httpClientFactory.CreateClient(CommunitiesClient);
        autheoClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        toolLibClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        soupKitchenClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        communitiesClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var meTask = autheoClient.GetAsync("/api/auth/me", ct);
        var statsTask = toolLibClient.GetAsync("/api/dashboard/stats", ct);
        var soupKitchenStatsTask = soupKitchenClient.GetAsync("/api/dashboard/stats", ct);
        var communitiesStatsTask = communitiesClient.GetAsync("/api/communities/dashboard/stats", ct);

        await Task.WhenAll(meTask, statsTask, soupKitchenStatsTask, communitiesStatsTask);

        var meResponse = await meTask;
        var statsResponse = await statsTask;
        var soupKitchenStatsResponse = await soupKitchenStatsTask;
        var communitiesStatsResponse = await communitiesStatsTask;

        if (!meResponse.IsSuccessStatusCode)
            return Unauthorized();

        var user = await meResponse.Content.ReadFromJsonAsync<UserInfo>(cancellationToken: ct);
        var stats = statsResponse.IsSuccessStatusCode
            ? await statsResponse.Content.ReadFromJsonAsync<DashboardStats>(cancellationToken: ct)
                ?? new DashboardStats(0, 0, 0, 0, 0)
            : new DashboardStats(0, 0, 0, 0, 0);
        var soupKitchenStats = soupKitchenStatsResponse.IsSuccessStatusCode
            ? await soupKitchenStatsResponse.Content.ReadFromJsonAsync<SoupKitchenDashboardStats>(cancellationToken: ct)
                ?? new SoupKitchenDashboardStats(0, 0, 0, 0, 0)
            : new SoupKitchenDashboardStats(0, 0, 0, 0, 0);
        var communitiesStats = communitiesStatsResponse.IsSuccessStatusCode
            ? await communitiesStatsResponse.Content.ReadFromJsonAsync<CommunitiesDashboardStats>(cancellationToken: ct)
                ?? new CommunitiesDashboardStats(0, 0, 0, 0, 0)
            : new CommunitiesDashboardStats(0, 0, 0, 0, 0);

        return Ok(new DashboardResponse(user!, stats, soupKitchenStats, communitiesStats));
    }
}