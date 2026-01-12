using LearningFlashCards.Api.Controllers;
using LearningFlashCards.Api.Controllers.Requests;
using LearningFlashCards.Api.Services;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Api.Tests.TestUtilities;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using LearningFlashCards.Core.Application.Abstractions.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LearningFlashCards.Api.Tests.Controllers;

public class TagsControllerTests
{
    [Fact]
    public async Task GetTags_ReturnsBadRequest_WhenHeaderMissing()
    {
        var handler = new TagsHandler(Mock.Of<ITagRepository>());
        var controller = new TagsController(handler)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetTags(CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpsertTag_ReturnsBadRequest_WhenHeaderMissing()
    {
        var handler = new TagsHandler(Mock.Of<ITagRepository>());
        var controller = new TagsController(handler)
        {
            ControllerContext = ControllerContextFactory.WithoutOwner()
        };

        var request = new UpsertTagRequest { Name = "Tag" };
        var result = await controller.UpsertTag(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpsertTag_ReturnsOk_WhenOwned()
    {
        using var dbContext = Api.Tests.TestUtilities.TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var handler = new TagsHandler(repository);
        var ownerId = Guid.NewGuid();

        var controller = new TagsController(handler)
        {
            ControllerContext = ControllerContextFactory.WithOwner(ownerId)
        };

        var request = new UpsertTagRequest
        {
            Name = "Tag"
        };

        var result = await controller.UpsertTag(request, CancellationToken.None);

        var ok = Assert.IsType<ObjectResult>(result.Result);
        var saved = Assert.IsType<Tag>(ok.Value);
        Assert.Equal(ownerId, saved.OwnerId);
    }

    [Fact]
    public async Task SoftDeleteTag_ReturnsNotFound_WhenHandlerNotFound()
    {
        var repoMock = new Mock<ITagRepository>();
        repoMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tag?)null);
        var handler = new TagsHandler(repoMock.Object);

        var controller = new TagsController(handler)
        {
            ControllerContext = ControllerContextFactory.WithOwner(Guid.NewGuid())
        };

        var result = await controller.SoftDeleteTag(Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }
}
