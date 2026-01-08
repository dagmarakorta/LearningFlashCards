using LearningFlashCards.Core.Application.Abstractions.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    public partial class LoginPage : ContentPage
    {
        private readonly IUserProfileRepository _userRepository;
        private readonly ICurrentUserService _currentUser;

        public LoginPage()
        {
            InitializeComponent();

            _userRepository = GetRequiredService<IUserProfileRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();
        }

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            var email = EmailEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                await DisplayAlertAsync("Missing email", "Please enter your email.", "OK");
                return;
            }

            var normalizedEmail = email.ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(normalizedEmail, CancellationToken.None);
            if (user is null)
            {
                await DisplayAlertAsync("Not found", "No account found for that email.", "OK");
                return;
            }

            _currentUser.SetUser(user.Id);
            await Shell.Current.GoToAsync("//MainPage");
        }

        private async void OnRegisterClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(RegisterPage));
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
