using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using LearningFlashCards.Api.Services;

namespace LearningFlashCards.Api.Controllers;

[Route("api/[controller]")]
public class UsersController : ApiControllerBase
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly CreateUserProfileHandler _createUserProfileHandler;

    public UsersController(IUserProfileRepository userProfileRepository, CreateUserProfileHandler createUserProfileHandler)
    {
        _userProfileRepository = userProfileRepository;
        _createUserProfileHandler = createUserProfileHandler;
    }

    [HttpPost]
    public async Task<ActionResult<UserProfile>> CreateProfile([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _createUserProfileHandler.HandleAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Error);
        }

        Response.Headers.Append("X-Owner-Id", result.Profile!.Id.ToString());
        return Created("/api/users/me", result.Profile);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfile>> GetProfile(CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var profile = await _userProfileRepository.GetAsync(ownerId, cancellationToken);
        if (profile is null || profile.DeletedAt != null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfile>> UpsertProfile([FromBody] UserProfile profile, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        profile.Id = ownerId;
        profile.ModifiedAt = DateTimeOffset.UtcNow;
        await _userProfileRepository.UpsertAsync(profile, cancellationToken);

        return Ok(profile);
    }
}
