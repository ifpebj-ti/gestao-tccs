using System.Security.Claims;
using gestaotcc.WebApi.Controllers;
using gestaotcc.WebApi.InputModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace gestaotcc.Test.Controllers;

public class TccAuthenticationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public async Task InvalidIdentityIsRejectedBeforeDepartureInputValidation(string? userId)
    {
        var controller = CreateController(userId);
        var result = await controller.RequestAdvisorDeparture(1, new RequestAdvisorDepartureInputModel("", 0), null!);
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public async Task InvalidIdentityIsRejectedBeforeUploadValidation(string? userId)
    {
        var result = await CreateController(userId).UploadFinalTccFile(1, null!, null!);
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task AuthenticatedRequestsStillValidateInput()
    {
        var controller = CreateController("1");
        var departure = await controller.RequestAdvisorDeparture(1, new RequestAdvisorDepartureInputModel("", 0), null!);
        var upload = await controller.UploadFinalTccFile(1, null!, null!);
        Assert.IsType<BadRequestObjectResult>(departure.Result);
        Assert.IsType<BadRequestObjectResult>(upload.Result);
    }

    private static TccController CreateController(string? userId) => new()
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    userId is null ? Array.Empty<Claim>() : new[] { new Claim("userId", userId) }, "test"))
            }
        }
    };
}
