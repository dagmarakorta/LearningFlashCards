using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    [QueryProperty(nameof(DeckId), "deckId")]
    public partial class EditDeckPage : ContentPage
    {
        private readonly IDeckRepository _deckRepository;
        private readonly ICurrentUserService _currentUser;

        private Guid? _deckId;
        private Deck? _deck;

        public string? DeckId
        {
            get => _deckId?.ToString();
            set
            {
                if (Guid.TryParse(value, out var id))
                {
                    _deckId = id;
                }
            }
        }

        public EditDeckPage()
        {
            InitializeComponent();

            _deckRepository = GetRequiredService<IDeckRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDeckAsync();
        }

        private async Task LoadDeckAsync()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            if (_deckId is null)
            {
                await DisplayAlertAsync("Missing deck", "Unable to load deck.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            _deck = await _deckRepository.GetAsync(_deckId.Value, CancellationToken.None);
            if (_deck is null || _deck.OwnerId != _currentUser.UserId.Value)
            {
                await DisplayAlertAsync("Not found", "Deck not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            NameEntry.Text = _deck.Name;
            DescriptionEditor.Text = _deck.Description;
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            var name = NameEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlertAsync("Missing name", "Please enter a deck name.", "OK");
                return;
            }

            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await DisplayAlertAsync("Not signed in", "Please login to edit decks.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            if (_deck is null)
            {
                await DisplayAlertAsync("Missing data", "Unable to save changes.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            _deck.Name = name;
            _deck.Description = DescriptionEditor.Text?.Trim();
            _deck.ModifiedAt = DateTimeOffset.UtcNow;

            await _deckRepository.UpsertAsync(_deck, CancellationToken.None);
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
    }
}
