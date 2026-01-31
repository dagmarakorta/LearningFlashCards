using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    public partial class CreateDeckPage : ContentPage
    {
        private readonly IDeckRepository _deckRepository;
        private readonly ICurrentUserService _currentUser;

        public CreateDeckPage()
        {
            InitializeComponent();

            _deckRepository = GetRequiredService<IDeckRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();

            ApplyDefaults(new DeckStudySettings());
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            var name = NameEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlertAsync("Missing name", "Please enter a deck name.", "OK");
                return;
            }

            var settings = await ReadStudySettingsAsync();
            if (settings is null)
            {
                return;
            }

            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await DisplayAlertAsync("Not signed in", "Please login to create a deck.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var deck = new Deck
            {
                OwnerId = _currentUser.UserId.Value,
                Name = name,
                Description = DescriptionEditor.Text?.Trim(),
                StudySettings = settings
            };

            await _deckRepository.UpsertAsync(deck, CancellationToken.None);
            await Shell.Current.GoToAsync("..");
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

        private void ApplyDefaults(DeckStudySettings settings)
        {
            DailyLimitEntry.Text = settings.DailyReviewLimit.ToString();
            EasyMinIntervalEntry.Text = settings.EasyMinIntervalDays.ToString();
            MaxIntervalEntry.Text = settings.MaxIntervalDays.ToString();
            RepeatInSessionSwitch.IsToggled = settings.RepeatInSession;
        }

        private async Task<DeckStudySettings?> ReadStudySettingsAsync()
        {
            var settings = new DeckStudySettings();

            if (!TryReadPositiveInt(DailyLimitEntry.Text, out var dailyLimit))
            {
                await DisplayAlertAsync("Invalid settings", "Daily review limit must be a positive number.", "OK");
                return null;
            }

            if (!TryReadPositiveInt(EasyMinIntervalEntry.Text, out var easyMin))
            {
                await DisplayAlertAsync("Invalid settings", "Easy minimum interval must be a positive number.", "OK");
                return null;
            }

            if (!TryReadPositiveInt(MaxIntervalEntry.Text, out var maxInterval))
            {
                await DisplayAlertAsync("Invalid settings", "Max interval must be a positive number.", "OK");
                return null;
            }

            if (maxInterval < easyMin)
            {
                await DisplayAlertAsync("Invalid settings", "Max interval must be greater than or equal to the easy minimum interval.", "OK");
                return null;
            }

            settings.DailyReviewLimit = dailyLimit;
            settings.EasyMinIntervalDays = easyMin;
            settings.MaxIntervalDays = maxInterval;
            settings.RepeatInSession = RepeatInSessionSwitch.IsToggled;

            return settings;
        }

        private static bool TryReadPositiveInt(string? text, out int value)
        {
            if (!int.TryParse(text, out value) || value <= 0)
            {
                value = 0;
                return false;
            }

            return true;
        }
    }
}
