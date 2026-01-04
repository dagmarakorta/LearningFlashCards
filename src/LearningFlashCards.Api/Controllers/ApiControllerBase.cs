using Microsoft.AspNetCore.Mvc;

namespace LearningFlashCards.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected bool TryGetOwnerId(out Guid ownerId)
    {
        if (Request.Headers.TryGetValue("X-Owner-Id", out var ownerHeader) &&
            Guid.TryParse(ownerHeader, out ownerId) &&
            ownerId != Guid.Empty)
        {
            return true;
        }

        ownerId = Guid.Empty;
        return false;
    }
}
