using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(CreateDeckPage), typeof(CreateDeckPage));
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
            Routing.RegisterRoute(nameof(ChangeEmailPage), typeof(ChangeEmailPage));
            Routing.RegisterRoute(nameof(DeckDetailPage), typeof(DeckDetailPage));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var currentUser = GetRequiredService<ICurrentUserService>();
            var target = currentUser.IsAuthenticated ? "//MainPage" : "//LoginPage";
            if (CurrentState.Location.OriginalString != target)
            {
                await GoToAsync(target);
            }
        }

        private static T GetRequiredService<T>() where T : notnull
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services is null)
            {
                throw new InvalidOperationException("Services are not available.");
            }

            return services.GetRequiredService<T>();
        }
    }
}
