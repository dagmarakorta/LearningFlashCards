using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace LearningFlashCards.Maui
{
    public partial class ProfilePage : ContentPage
    {
        private readonly IUserProfileRepository _userRepository;
        private readonly ICurrentUserService _currentUser;
        private UserProfile? _profile;

        public ProfilePage()
        {
            InitializeComponent();

            _userRepository = GetRequiredService<IUserProfileRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            _profile = await _userRepository.GetAsync(_currentUser.UserId.Value, CancellationToken.None);
            if (_profile is null)
            {
                _currentUser.Clear();
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            NameEntry.Text = _profile.DisplayName;
            EmailEntry.Text = _profile.Email;
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (_profile is null)
            {
                await DisplayAlertAsync("Missing profile", "Please login again.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

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
            var emailOwner = await _userRepository.GetByEmailAsync(normalizedEmail, CancellationToken.None);
            if (emailOwner is not null && emailOwner.Id != _profile.Id)
            {
                await DisplayAlertAsync("Email already used", "Please use a different email.", "OK");
                return;
            }

            var nameOwner = await _userRepository.GetByDisplayNameAsync(name, CancellationToken.None);
            if (nameOwner is not null && nameOwner.Id != _profile.Id)
            {
                await DisplayAlertAsync("Name already used", "Please choose a different name.", "OK");
                return;
            }

            _profile.DisplayName = name;
            _profile.Email = normalizedEmail;

            await _userRepository.UpsertAsync(_profile, CancellationToken.None);
            await DisplayAlertAsync("Saved", "Profile updated.", "OK");
        }

        private async void OnLogoutClicked(object? sender, EventArgs e)
        {
            _currentUser.Clear();
            await Shell.Current.GoToAsync("//LoginPage");
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
