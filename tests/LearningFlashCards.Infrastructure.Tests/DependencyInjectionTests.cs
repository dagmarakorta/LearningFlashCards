using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LearningFlashCards.Infrastructure.Tests;

public class DependencyInjectionTests
{
    private static IConfiguration CreateConfiguration(string? connectionString = null)
    {
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c.GetSection("ConnectionStrings")["Default"])
            .Returns(connectionString);
        return configMock.Object;
    }

    [Fact]
    public void AddInfrastructure_RegistersAllServices()
    {
        var configuration = CreateConfiguration("Data Source=:memory:");

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<AppDbContext>());
        Assert.NotNull(provider.GetService<ICardRepository>());
        Assert.NotNull(provider.GetService<IDeckRepository>());
        Assert.NotNull(provider.GetService<ITagRepository>());
        Assert.NotNull(provider.GetService<IUserProfileRepository>());
    }

    [Fact]
    public void AddInfrastructure_RegistersRepositoriesAsScoped()
    {
        var configuration = CreateConfiguration("Data Source=:memory:");

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        var cardDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICardRepository));
        var deckDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDeckRepository));
        var tagDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITagRepository));
        var userProfileDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IUserProfileRepository));

        Assert.NotNull(cardDescriptor);
        Assert.NotNull(deckDescriptor);
        Assert.NotNull(tagDescriptor);
        Assert.NotNull(userProfileDescriptor);

        Assert.Equal(ServiceLifetime.Scoped, cardDescriptor!.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, deckDescriptor!.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, tagDescriptor!.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, userProfileDescriptor!.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_UsesDefaultConnectionString_WhenNotConfigured()
    {
        var configuration = CreateConfiguration(null);

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var dbContext = provider.GetService<AppDbContext>();
        Assert.NotNull(dbContext);
    }

    [Fact]
    public void AddInfrastructure_ScopedServices_ReturnSameInstanceWithinScope()
    {
        var configuration = CreateConfiguration("Data Source=:memory:");

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var repo1 = scope.ServiceProvider.GetService<ICardRepository>();
        var repo2 = scope.ServiceProvider.GetService<ICardRepository>();

        Assert.Same(repo1, repo2);
    }

    [Fact]
    public void AddInfrastructure_ScopedServices_ReturnDifferentInstancesAcrossScopes()
    {
        var configuration = CreateConfiguration("Data Source=:memory:");

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        ICardRepository? repo1;
        ICardRepository? repo2;

        using (var scope1 = provider.CreateScope())
        {
            repo1 = scope1.ServiceProvider.GetService<ICardRepository>();
        }

        using (var scope2 = provider.CreateScope())
        {
            repo2 = scope2.ServiceProvider.GetService<ICardRepository>();
        }

        Assert.NotSame(repo1, repo2);
    }
}
