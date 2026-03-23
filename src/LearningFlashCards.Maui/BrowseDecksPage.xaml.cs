using System.Collections.ObjectModel;
using System.Linq;
using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    public partial class BrowseDecksPage : ContentPage
    {
        private const double CardHorizontalPadding = 40;
        private const double CardMaxWidth = 1180;

        private readonly IDeckRepository _deckRepository;
        private readonly ICardRepository _cardRepository;
        private readonly ICurrentUserService _currentUser;

        public ObservableCollection<DeckBrowseItem> Decks { get; } = new();

        public BrowseDecksPage()
        {
            InitializeComponent();
            BindingContext = this;

            _deckRepository = GetRequiredService<IDeckRepository>();
            _cardRepository = GetRequiredService<ICardRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();

            SizeChanged += OnPageSizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            UpdateCardWidth();
            await LoadDecksAsync();
        }

        private async Task LoadDecksAsync()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var decks = (await _deckRepository.GetByOwnerAsync(_currentUser.UserId.Value, CancellationToken.None))
                .OrderBy(deck => deck.Name)
                .ToList();

            var cardTasks = decks.Select(async deck => new
            {
                Deck = deck,
                Cards = await _cardRepository.GetByDeckAsync(deck.Id, CancellationToken.None)
            });

            var loadedDecks = await Task.WhenAll(cardTasks);

            Decks.Clear();
            foreach (var (item, index) in loadedDecks.Select((item, index) => (item, index)))
            {
                var summary = string.IsNullOrWhiteSpace(item.Deck.Description)
                    ? "Ready for another round"
                    : item.Deck.Description.Trim();

                Decks.Add(new DeckBrowseItem(
                    item.Deck.Id,
                    GetDeckMonogram(item.Deck.Name),
                    item.Deck.Name,
                    summary,
                    item.Cards.Count));
            }
        }

        private static string GetDeckMonogram(string? deckName)
        {
            var firstCharacter = deckName?.Trim().FirstOrDefault(static c => char.IsLetterOrDigit(c));
            if (firstCharacter is null || firstCharacter == default)
            {
                return "D";
            }

            return char.ToUpperInvariant(firstCharacter.Value).ToString();
        }

        private async void OnDeckTapped(object? sender, TappedEventArgs e)
        {
            if (sender is not Border border || border.BindingContext is not DeckBrowseItem deck)
            {
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(DeckDetailPage)}?deckId={deck.Id}");
        }

        private async void OnNewDeckClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(CreateDeckPage));
        }

        private async void OnEditDeckClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.BindingContext is not DeckBrowseItem deck)
            {
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(EditDeckPage)}?deckId={deck.Id}");
        }

        private async void OnDeleteDeckClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.BindingContext is not DeckBrowseItem deck)
            {
                return;
            }

            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var confirm = await AppDialogService.ShowConfirmAsync(this, "Delete deck", "Delete this deck and all its cards?", "Delete");
            if (!confirm)
            {
                return;
            }

            var existing = await _deckRepository.GetAsync(deck.Id, CancellationToken.None);
            if (existing is null || existing.OwnerId != _currentUser.UserId.Value)
            {
                await AppDialogService.ShowAlertAsync(this, "Not found", "Deck not found.");
                return;
            }

            var deletedAt = DateTimeOffset.UtcNow;
            await _cardRepository.SoftDeleteByDeckAsync(deck.Id, deletedAt, CancellationToken.None);
            await _deckRepository.SoftDeleteAsync(deck.Id, deletedAt, CancellationToken.None);
            Decks.Remove(deck);
        }

        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            UpdateCardWidth();
        }

        private void UpdateCardWidth()
        {
            if (Width <= 0)
            {
                return;
            }

            var availableWidth = Math.Max(0, Width - CardHorizontalPadding);
            var cardWidth = Math.Min(CardMaxWidth, availableWidth);

            CardHost.WidthRequest = cardWidth;
            DecksCard.WidthRequest = cardWidth;
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

        public sealed record DeckBrowseItem(Guid Id, string Icon, string Name, string Summary, int CardCount);
    }
}
