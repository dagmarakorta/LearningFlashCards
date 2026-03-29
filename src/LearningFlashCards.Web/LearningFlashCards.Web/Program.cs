using LearningFlashCards.Web.Client.Services;
using LearningFlashCards.Web.Client;
using LearningFlashCards.Api.Controllers;
using LearningFlashCards.Api.Services;
using LearningFlashCards.Infrastructure;
using LearningFlashCards.Infrastructure.Persistence;
using LearningFlashCards.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddControllers()
    .AddApplicationPart(typeof(UsersController).Assembly);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateUserProfileHandler>();
builder.Services.AddScoped<DeckHandler>();
builder.Services.AddScoped<CardsHandler>();
builder.Services.AddScoped<TagsHandler>();
builder.Services.AddScoped(sp =>
{
    var navigation = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navigation.BaseUri) };
});
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<ApiClient>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(LearningFlashCards.Web.Client._Imports).Assembly);

app.Run();
