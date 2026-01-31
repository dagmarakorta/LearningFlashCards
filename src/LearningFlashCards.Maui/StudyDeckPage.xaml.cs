using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Study;
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
        private DeckStudySettings _studySettings = new();

        public string DeckName { get; private set; } = "Study";
        public string CurrentSideText { get; private set; } = string.Empty;
        public string ProgressText { get; private set; } = string.Empty;
        public bool HasCards { get; private set; }
        public bool CanShowAnswer { get; private set; }
        public bool CanRate { get; private set; }

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

            _studySettings = deck.StudySettings ?? new DeckStudySettings();

            var cards = await _cardRepository.GetByDeckAsync(deck.Id, CancellationToken.None);
            var now = DateTimeOffset.UtcNow;
            var dailyLimit = Math.Max(0, _studySettings.DailyReviewLimit);
            var dueCards = StudyQueueBuilder.SelectDue(cards, now, dailyLimit);

            _cards.Clear();
            _cards.AddRange(dueCards);
            _currentIndex = 0;
            _showBack = false;

            UpdateCardDisplay();
        }

        private void UpdateCardDisplay()
        {
            HasCards = _cards.Count > 0;
            CanShowAnswer = HasCards && !_showBack;
            CanRate = HasCards && _showBack;

            if (!HasCards)
            {
                CurrentSideText = "No cards due yet.";
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
            OnPropertyChanged(nameof(HasCards));
            OnPropertyChanged(nameof(CanShowAnswer));
            OnPropertyChanged(nameof(CanRate));
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

        private void OnShowAnswerClicked(object? sender, EventArgs e)
        {
            if (_cards.Count == 0)
            {
                return;
            }

            _showBack = true;
            UpdateCardDisplay();
        }

        private async void OnRateAgainClicked(object? sender, EventArgs e)
        {
            await RateCurrentCardAsync(CardReviewRating.Again);
        }

        private async void OnRateHardClicked(object? sender, EventArgs e)
        {
            await RateCurrentCardAsync(CardReviewRating.Hard);
        }

        private async void OnRateMediumClicked(object? sender, EventArgs e)
        {
            await RateCurrentCardAsync(CardReviewRating.Medium);
        }

        private async void OnRateEasyClicked(object? sender, EventArgs e)
        {
            await RateCurrentCardAsync(CardReviewRating.Easy);
        }

        private async Task RateCurrentCardAsync(CardReviewRating rating)
        {
            if (_cards.Count == 0)
            {
                return;
            }

            var card = _cards[_currentIndex];
            var now = DateTimeOffset.UtcNow;

            SpacedRepetitionScheduler.ApplyRating(card.State, rating, now, _studySettings);
            card.ModifiedAt = now;
            await _cardRepository.UpsertAsync(card, CancellationToken.None);

            var repeatInSession = StudySessionRules.ShouldRepeatInSession(rating, _studySettings);

            _cards.RemoveAt(_currentIndex);
            if (repeatInSession)
            {
                _cards.Add(card);
            }

            if (_cards.Count == 0)
            {
                await DisplayAlertAsync("Done", "You've completed all due cards for now.", "OK");
                _currentIndex = 0;
                _showBack = false;
                UpdateCardDisplay();
                return;
            }

            if (_currentIndex >= _cards.Count)
            {
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
