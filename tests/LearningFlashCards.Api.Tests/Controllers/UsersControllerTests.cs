using LearningFlashCards.Api.Controllers;
using LearningFlashCards.Api.Services;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Api.Tests.TestUtilities;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using LearningFlashCards.Core.Application.Abstractions.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LearningFlashCards.Api.Tests.Controllers;

public class UsersControllerTests
{
    [Fact]
    public async Task CreateProfile_ReturnsCreated_WhenHandlerSucceeds()
    {
        var repoMock = new Mock<IUserProfileRepository>();
        repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var createdProfile = new UserProfile { Id = Guid.NewGuid(), Email = "a@b.com", DisplayName = "Test" };
        repoMock.Setup(r => r.CreateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdProfile);
        var handler = new CreateUserProfileHandler(repoMock.Object);

        var controller = new UsersController(repoMock.Object, handler)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        var result = await controller.CreateProfile(new CreateUserRequest { DisplayName = "t", Email = "e@x.com" }, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(createdProfile, created.Value);
        Assert.True(controller.Response.Headers.ContainsKey("X-Owner-Id"));
    }

    [Fact]
    public async Task CreateProfile_ReturnsStatus_WhenHandlerFails()
    {
        var repoMock = new Mock<IUserProfileRepository>();
        repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new CreateUserProfileHandler(repoMock.Object);

        var controller = new UsersController(repoMock.Object, handler)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        var result = await controller.CreateProfile(new CreateUserRequest { DisplayName = "t", Email = "e@x.com" }, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, status.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ReturnsBadRequest_WhenHeaderMissing()
    {
        var repoMock = new Mock<IUserProfileRepository>();
        var controller = new UsersController(repoMock.Object, new CreateUserProfileHandler(repoMock.Object));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.GetProfile(CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetProfile_ReturnsProfile_WhenFound()
    {
        using var dbContext = Api.Tests.TestUtilities.TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var handler = new CreateUserProfileHandler(repository);
        var controller = new UsersController(repository, handler)
        {
            ControllerContext = ControllerContextFactory.WithOwner(Guid.NewGuid())
        };

        var ownerId = Guid.Parse(controller.HttpContext.Request.Headers["X-Owner-Id"]!);
        await repository.CreateAsync(new UserProfile
        {
            Id = ownerId,
            DisplayName = "Owner",
            Email = "owner@example.com"
        }, CancellationToken.None);

        var result = await controller.GetProfile(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var profile = Assert.IsType<UserProfile>(ok.Value);
        Assert.Equal(ownerId, profile.Id);
    }

    [Fact]
    public async Task UpsertProfile_SetsOwnerFromHeader()
    {
        using var dbContext = Api.Tests.TestUtilities.TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var handler = new CreateUserProfileHandler(repository);
        var controller = new UsersController(repository, handler)
        {
            ControllerContext = ControllerContextFactory.WithOwner(Guid.NewGuid())
        };

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            DisplayName = "New",
            Email = "new@example.com"
        };

        var result = await controller.UpsertProfile(profile, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<UserProfile>(ok.Value);
        var ownerId = Guid.Parse(controller.HttpContext.Request.Headers["X-Owner-Id"]!);
        Assert.Equal(ownerId, updated.Id);
    }

    [Fact]
    public async Task UpsertProfile_ReturnsBadRequest_WhenHeaderMissing()
    {
        using var dbContext = Api.Tests.TestUtilities.TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var handler = new CreateUserProfileHandler(repository);
        var controller = new UsersController(repository, handler)
        {
            ControllerContext = ControllerContextFactory.WithoutOwner()
        };

        var result = await controller.UpsertProfile(new UserProfile(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
