using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    [QueryProperty(nameof(DeckId), "deckId")]
    public partial class StudyDeckPage : ContentPage
    {
        private readonly IDeckRepository _deckRepository;
        private readonly ICardRepository _cardRepository;
        private readonly ICurrentUserService _currentUser;

        private Guid? _deckId;
        private readonly List<Card> _cards = new();
        private int _currentIndex;
        private bool _showBack;

        public string DeckName { get; private set; } = "Study";
        public string CurrentSideText { get; private set; } = string.Empty;
        public string ProgressText { get; private set; } = string.Empty;

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

        public StudyDeckPage()
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
            await LoadStudyDeckAsync();
        }

        private async Task LoadStudyDeckAsync()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            if (_deckId is null)
            {
                await DisplayAlertAsync("Missing deck", "Unable to study without a deck.", "OK");
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
            OnPropertyChanged(nameof(DeckName));

            var cards = await _cardRepository.GetByDeckAsync(deck.Id, CancellationToken.None);
            _cards.Clear();
            _cards.AddRange(cards);
            _currentIndex = 0;
            _showBack = false;

            UpdateCardDisplay();
        }

        private void UpdateCardDisplay()
        {
            if (_cards.Count == 0)
            {
                CurrentSideText = "No cards yet.";
                ProgressText = string.Empty;
            }
            else
            {
                var card = _cards[_currentIndex];
                CurrentSideText = _showBack ? card.Back : card.Front;
                ProgressText = $"{_currentIndex + 1} / {_cards.Count}";
            }

            OnPropertyChanged(nameof(CurrentSideText));
            OnPropertyChanged(nameof(ProgressText));
        }

        private void OnFlipTapped(object? sender, TappedEventArgs e)
        {
            if (_cards.Count == 0)
            {
                return;
            }

            _showBack = !_showBack;
            UpdateCardDisplay();
        }

        private async void OnNextClicked(object? sender, EventArgs e)
        {
            if (_cards.Count == 0)
            {
                await Shell.Current.GoToAsync("..");
                return;
            }

            _currentIndex++;
            if (_currentIndex >= _cards.Count)
            {
                await DisplayAlertAsync("Done", "You've reached the end of this deck.", "OK");
                _currentIndex = 0;
            }

            _showBack = false;
            UpdateCardDisplay();
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
