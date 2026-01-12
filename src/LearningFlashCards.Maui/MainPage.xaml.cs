using System.Collections.ObjectModel;
using System.Linq;
using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<DeckListItem> Decks { get; } = new();
        private readonly IDeckRepository _deckRepository;
        private readonly ICardRepository _cardRepository;
        private readonly ICurrentUserService _currentUser;
        private bool _suppressSelection;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            _deckRepository = GetRequiredService<IDeckRepository>();
            _cardRepository = GetRequiredService<ICardRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDecksAsync();
        }

        private async void OnNewDeckClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(CreateDeckPage));
        }

        private async void OnProfileClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ProfilePage));
        }

        private async void OnDeckSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not CollectionView collection)
            {
                return;
            }

            if (_suppressSelection)
            {
                _suppressSelection = false;
                collection.SelectedItem = null;
                return;
            }

            if (e.CurrentSelection.FirstOrDefault() is not DeckListItem deck)
            {
                return;
            }

            collection.SelectedItem = null;
            await Shell.Current.GoToAsync($"{nameof(DeckDetailPage)}?deckId={deck.Id}");
        }

        private async void OnDeleteDeckClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.BindingContext is not DeckListItem deck)
            {
                return;
            }

            _suppressSelection = true;

            var confirm = await DisplayAlertAsync("Delete deck", "Delete this deck and all its cards?", "Delete", "Cancel");
            if (!confirm)
            {
                return;
            }

            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var existing = await _deckRepository.GetAsync(deck.Id, CancellationToken.None);
            if (existing is null || existing.OwnerId != _currentUser.UserId.Value)
            {
                await DisplayAlertAsync("Not found", "Deck not found.", "OK");
                return;
            }

            var deletedAt = DateTimeOffset.UtcNow;
            await _cardRepository.SoftDeleteByDeckAsync(deck.Id, deletedAt, CancellationToken.None);
            await _deckRepository.SoftDeleteAsync(deck.Id, deletedAt, CancellationToken.None);
            Decks.Remove(deck);
        }

        public record DeckListItem(Guid Id, string Name, string Summary);

        private async Task LoadDecksAsync()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var decks = await _deckRepository.GetByOwnerAsync(_currentUser.UserId.Value, CancellationToken.None);
            Decks.Clear();

            foreach (var deck in decks.OrderBy(d => d.Name))
            {
                Decks.Add(new DeckListItem(deck.Id, deck.Name, BuildSummary(deck)));
            }
        }

        private static string BuildSummary(Deck deck)
        {
            if (!string.IsNullOrWhiteSpace(deck.Description))
            {
                return deck.Description;
            }

            return "No description";
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
