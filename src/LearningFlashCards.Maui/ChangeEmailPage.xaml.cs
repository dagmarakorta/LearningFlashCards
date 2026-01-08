using LearningFlashCards.Core.Application.Abstractions.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace LearningFlashCards.Maui
{
    public partial class ChangeEmailPage : ContentPage
    {
        private readonly IUserProfileRepository _userRepository;
        private readonly ICurrentUserService _currentUser;

        public ChangeEmailPage()
        {
            InitializeComponent();

            _userRepository = GetRequiredService<IUserProfileRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await DisplayAlertAsync("Not signed in", "Please login first.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var email = EmailEntry.Text?.Trim();
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
            var owner = await _userRepository.GetByEmailAsync(normalizedEmail, CancellationToken.None);
            if (owner is not null && owner.Id != _currentUser.UserId.Value)
            {
                await DisplayAlertAsync("Email already used", "Please use a different email.", "OK");
                return;
            }

            var profile = await _userRepository.GetAsync(_currentUser.UserId.Value, CancellationToken.None);
            if (profile is null)
            {
                _currentUser.Clear();
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            profile.Email = normalizedEmail;
            await _userRepository.UpsertAsync(profile, CancellationToken.None);
            await DisplayAlertAsync("Email updated", "Please login again with your new email.", "OK");
            _currentUser.Clear();
            await Shell.Current.GoToAsync("//LoginPage");
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
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
    }
}
