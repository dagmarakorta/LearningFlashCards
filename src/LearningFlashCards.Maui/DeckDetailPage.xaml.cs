using System.Collections.ObjectModel;
using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    [QueryProperty(nameof(DeckId), "deckId")]
    public partial class DeckDetailPage : ContentPage
    {
        private readonly IDeckRepository _deckRepository;
        private readonly ICardRepository _cardRepository;
        private readonly ICurrentUserService _currentUser;

        private Guid? _deckId;

        public ObservableCollection<CardListItem> Cards { get; } = new();
        public string DeckName { get; private set; } = "Deck";
        public string DeckDescription { get; private set; } = string.Empty;

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

        public DeckDetailPage()
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

            var deck = await _deckRepository.GetAsync(_deckId.Value, CancellationToken.None);
            if (deck is null || deck.OwnerId != _currentUser.UserId.Value)
            {
                await DisplayAlertAsync("Not found", "Deck not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            DeckName = deck.Name;
            DeckDescription = string.IsNullOrWhiteSpace(deck.Description) ? "No description" : deck.Description;
            OnPropertyChanged(nameof(DeckName));
            OnPropertyChanged(nameof(DeckDescription));

            var cards = await _cardRepository.GetByDeckAsync(deck.Id, CancellationToken.None);
            Cards.Clear();
            foreach (var card in cards)
            {
                Cards.Add(new CardListItem(card.Id, card.Front, card.Back));
            }
        }

        private async void OnAddCardClicked(object? sender, EventArgs e)
        {
            if (_deckId is null)
            {
                await DisplayAlertAsync("Missing deck", "Select a deck before adding a card.", "OK");
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(CreateCardPage)}?deckId={_deckId}");
        }

        private async void OnStudyClicked(object? sender, EventArgs e)
        {
            if (_deckId is null)
            {
                await DisplayAlertAsync("Missing deck", "Select a deck before studying.", "OK");
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(StudyDeckPage)}?deckId={_deckId}");
        }

        private async void OnDeleteCardClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.BindingContext is not CardListItem card)
            {
                return;
            }

            var confirm = await DisplayAlertAsync("Delete card", "Delete this card from the deck?", "Delete", "Cancel");
            if (!confirm)
            {
                return;
            }

            await _cardRepository.SoftDeleteAsync(card.Id, DateTimeOffset.UtcNow, CancellationToken.None);
            Cards.Remove(card);
        }

        private async void OnEditCardClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.BindingContext is not CardListItem card)
            {
                return;
            }

            if (_deckId is null)
            {
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(EditCardPage)}?deckId={_deckId}&cardId={card.Id}");
        }

        public record CardListItem(Guid Id, string Front, string Back);

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
