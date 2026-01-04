using LearningFlashCards.Api.Services;
using LearningFlashCards.Infrastructure;
using LearningFlashCards.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LearningFlashCards API",
        Version = "v1"
    });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateUserProfileHandler>();
builder.Services.AddScoped<DeckHandler>();
builder.Services.AddScoped<CardsHandler>();
builder.Services.AddScoped<TagsHandler>();

var app = builder.Build();

// Apply pending migrations on startup to ensure the database schema is up to date.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "LearningFlashCards API v1");
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
