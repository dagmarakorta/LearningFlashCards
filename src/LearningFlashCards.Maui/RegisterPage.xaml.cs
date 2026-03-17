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
                await AppDialogService.ShowAlertAsync(this, "Missing name", "Please enter your name.");
                return;
            }

            if (name.Length < 3)
            {
                await AppDialogService.ShowAlertAsync(this, "Name too short", "Name must be at least 3 characters.");
                return;
            }

            if (!NamePattern.IsMatch(name))
            {
                await AppDialogService.ShowAlertAsync(this, "Invalid name", "Use letters, spaces, apostrophes, or hyphens only.");
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                await AppDialogService.ShowAlertAsync(this, "Missing email", "Please enter your email.");
                return;
            }

            if (!EmailPattern.IsMatch(email))
            {
                await AppDialogService.ShowAlertAsync(this, "Invalid email", "Please enter a valid email address.");
                return;
            }

            var normalizedEmail = email.ToLowerInvariant();
            var exists = await _userRepository.ExistsByEmailAsync(normalizedEmail, CancellationToken.None);
            if (exists)
            {
                await AppDialogService.ShowAlertAsync(this, "Email already used", "Please login or use a different email.");
                return;
            }

            var nameExists = await _userRepository.ExistsByDisplayNameAsync(name, CancellationToken.None);
            if (nameExists)
            {
                await AppDialogService.ShowAlertAsync(this, "Name already used", "Please choose a different name.");
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
