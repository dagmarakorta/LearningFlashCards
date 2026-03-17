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
            EmailLabel.Text = _profile.Email;
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (_profile is null)
            {
                await AppDialogService.ShowAlertAsync(this, "Missing profile", "Please login again.");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var name = NameEntry.Text?.Trim();
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

            var nameOwner = await _userRepository.GetByDisplayNameAsync(name, CancellationToken.None);
            if (nameOwner is not null && nameOwner.Id != _profile.Id)
            {
                await AppDialogService.ShowAlertAsync(this, "Name already used", "Please choose a different name.");
                return;
            }

            _profile.DisplayName = name;

            await _userRepository.UpsertAsync(_profile, CancellationToken.None);
            await AppDialogService.ShowAlertAsync(this, "Saved", "Profile updated.");
        }

        private async void OnChangeEmailClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ChangeEmailPage));
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

        private static readonly Regex NamePattern = new(@"^[A-Za-z][A-Za-z' -]*$", RegexOptions.Compiled);
    }
}
