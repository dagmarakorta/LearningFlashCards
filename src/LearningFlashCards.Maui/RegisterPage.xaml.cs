using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace LearningFlashCards.Maui
{
    public partial class RegisterPage : ContentPage
    {
        private readonly IUserProfileRepository _userRepository;
        private readonly ICurrentUserService _currentUser;

        public RegisterPage()
        {
            InitializeComponent();

            _userRepository = GetRequiredService<IUserProfileRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();
        }

        private async void OnCreateClicked(object? sender, EventArgs e)
        {
            var name = NameEntry.Text?.Trim();
            var email = EmailEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlertAsync("Missing name", "Please enter your name.", "OK");
                return;
            }

            if (name.Length < 3)
            {
                await DisplayAlertAsync("Name too short", "Name must be at least 3 characters.", "OK");
                return;
            }

            if (!NamePattern.IsMatch(name))
            {
                await DisplayAlertAsync("Invalid name", "Use letters, spaces, apostrophes, or hyphens only.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                await DisplayAlertAsync("Missing email", "Please enter your email.", "OK");
                return;
            }

            if (!EmailPattern.IsMatch(email))
            {
                await DisplayAlertAsync("Invalid email", "Please enter a valid email address.", "OK");
                return;
            }

            var normalizedEmail = email.ToLowerInvariant();
            var exists = await _userRepository.ExistsByEmailAsync(normalizedEmail, CancellationToken.None);
            if (exists)
            {
                await DisplayAlertAsync("Email already used", "Please login or use a different email.", "OK");
                return;
            }

            var nameExists = await _userRepository.ExistsByDisplayNameAsync(name, CancellationToken.None);
            if (nameExists)
            {
                await DisplayAlertAsync("Name already used", "Please choose a different name.", "OK");
                return;
            }

            var profile = new UserProfile
            {
                DisplayName = name,
                Email = normalizedEmail
            };

            await _userRepository.CreateAsync(profile, CancellationToken.None);
            _currentUser.SetUser(profile.Id);
            await Shell.Current.GoToAsync("//MainPage");
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
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

        private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex NamePattern = new(@"^[A-Za-z][A-Za-z' -]*$", RegexOptions.Compiled);
    }
}
