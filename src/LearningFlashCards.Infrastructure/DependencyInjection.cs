using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Infrastructure.Persistence;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Default to a local SQLite database file for development.
                connectionString = "Data Source=learningflashcards.db";
            }

            options.UseSqlite(connectionString);
        });

        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<IDeckRepository, DeckRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        return services;
    }
}
