using System.Linq;
using LearningFlashCards.Api.Controllers;
using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace LearningFlashCards.Api.Services;

public class CreateUserProfileHandler
{
    private readonly IUserProfileRepository _userProfileRepository;

    public CreateUserProfileHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<CreateUserProfileResult> HandleAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var displayName = SanitizeText(request.DisplayName);
        var normalizedEmail = SanitizeText(request.Email).ToLowerInvariant();
        var avatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();

        if (HasUnsafeCharacters(displayName) || HasUnsafeCharacters(normalizedEmail))
        {
            return CreateUserProfileResult.Failure(StatusCodes.Status400BadRequest, "Input contains unsupported characters.");
        }

        if (avatarUrl is not null && !Uri.IsWellFormedUriString(avatarUrl, UriKind.Absolute))
        {
            return CreateUserProfileResult.Failure(StatusCodes.Status400BadRequest, "AvatarUrl must be an absolute URL.");
        }

        if (await _userProfileRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return CreateUserProfileResult.Failure(StatusCodes.Status409Conflict, "A user with this email already exists.");
        }

        var profile = new UserProfile
        {
            DisplayName = displayName,
            Email = normalizedEmail,
            AvatarUrl = avatarUrl
        };

        var createdProfile = await _userProfileRepository.CreateAsync(profile, cancellationToken);

        return CreateUserProfileResult.Success(createdProfile);
    }

    private static string SanitizeText(string value) => string.Concat(value.Where(c => !char.IsControl(c))).Trim();

    private static bool HasUnsafeCharacters(string value) =>
        value.IndexOfAny(new[] { '<', '>', '\\', '/', '`' }) >= 0;
}

public record CreateUserProfileResult(bool IsSuccess, UserProfile? Profile, int StatusCode, string? Error)
{
    public static CreateUserProfileResult Success(UserProfile profile) =>
        new(true, profile, StatusCodes.Status201Created, null);

    public static CreateUserProfileResult Failure(int statusCode, string error) =>
        new(false, null, statusCode, error);
}
