using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TSQR.Gateway.Application.Interfaces;

namespace TSQR.Gateway.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GatewayController : ControllerBase
    {
        private readonly IGatewayService _gatewayService;

        public GatewayController(IGatewayService gatewayService)
        {
            _gatewayService = gatewayService;
        }

        [HttpPost("route")]
        public async Task<IActionResult> RouteRequest([FromBody] RequestModel request)
        {
            var response = await _gatewayService.RouteRequest(request);
            return Ok(response);
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new { Status = "Gateway is running" });
        }
    }
}