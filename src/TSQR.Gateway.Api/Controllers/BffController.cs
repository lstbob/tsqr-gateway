using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSQR.Gateway.Api.Dtos;

namespace TSQR.Gateway.Api.Controllers;

[ApiController]
[Route("api/bff")]
[Authorize]
public sealed class BffController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public BffController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardResponse>> Dashboard(CancellationToken ct)
    {
        var token = HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var meTask = client.GetAsync("http://localhost:5068/api/auth/me", ct);
        var statsTask = client.GetAsync("http://localhost:5079/api/dashboard/stats", ct);

        await Task.WhenAll(meTask, statsTask);

        var meResponse = await meTask;
        var statsResponse = await statsTask;

        if (!meResponse.IsSuccessStatusCode)
            return Unauthorized();

        var user = await meResponse.Content.ReadFromJsonAsync<UserInfo>(cancellationToken: ct);
        var stats = statsResponse.IsSuccessStatusCode
            ? await statsResponse.Content.ReadFromJsonAsync<DashboardStats>(cancellationToken: ct) ?? new DashboardStats(0, 0, 0, 0, 0)
            : new DashboardStats(0, 0, 0, 0, 0);

        return Ok(new DashboardResponse(user!, stats));
    }
}
