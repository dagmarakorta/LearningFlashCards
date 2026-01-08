using LearningFlashCards.Infrastructure;
using LearningFlashCards.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LearningFlashCards.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "learningflashcards.db");

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={databasePath}"
            });

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddSingleton<ICurrentUserService, LocalCurrentUserService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            return app;
        }
    }
}
