using Xunit;
using TSQR.Gateway.Application.Interfaces;
using TSQR.Gateway.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace TSQR.Gateway.Tests
{
    public class GatewayTests
    {
        private readonly Mock<IGatewayService> _gatewayServiceMock;
        private readonly GatewayController _controller;

        public GatewayTests()
        {
            _gatewayServiceMock = new Mock<IGatewayService>();
            _controller = new GatewayController(_gatewayServiceMock.Object);
        }

        [Fact]
        public void Test_Get_ShouldReturnOkResult()
        {
            // Arrange
            var expectedResponse = "Success";
            _gatewayServiceMock.Setup(service => service.RouteRequest(It.IsAny<string>()))
                .Returns(expectedResponse);

            // Act
            var result = _controller.Get("test");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public void Test_Get_ShouldReturnNotFoundResult()
        {
            // Arrange
            _gatewayServiceMock.Setup(service => service.RouteRequest(It.IsAny<string>()))
                .Returns((string)null);

            // Act
            var result = _controller.Get("test");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}