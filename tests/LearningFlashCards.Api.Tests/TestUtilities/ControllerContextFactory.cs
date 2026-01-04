using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LearningFlashCards.Api.Tests.TestUtilities;

internal static class ControllerContextFactory
{
    public static ControllerContext WithOwner(Guid ownerId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Owner-Id"] = ownerId.ToString();

        return new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    public static ControllerContext WithoutOwner() => new()
    {
        HttpContext = new DefaultHttpContext()
    };
}
