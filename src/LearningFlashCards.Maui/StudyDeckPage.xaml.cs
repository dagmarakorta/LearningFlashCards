using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Study;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    [QueryProperty(nameof(DeckId), "deckId")]
    public partial class StudyDeckPage : ContentPage
    {
        private const int MaxTrackedSegmentSeconds = 120;
        private const int PersistThresholdSeconds = 15;

        private readonly IDeckRepository _deckRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly ICurrentUserService _currentUser;

        private Guid? _deckId;
        private readonly List<Card> _cards = new();
        private int _currentIndex;
        private bool _showBack;
        private DeckStudySettings _studySettings = new();
        private DateTimeOffset? _studySegmentStartedAt;
        private int _pendingTrackedStudySeconds;

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
            _userProfileRepository = GetRequiredService<IUserProfileRepository>();
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
                await AppDialogService.ShowAlertAsync(this, "Missing deck", "Unable to study without a deck.");
                await Shell.Current.GoToAsync("..");
                return;
            }

            var deck = await _deckRepository.GetAsync(_deckId.Value, CancellationToken.None);
            if (deck is null || deck.OwnerId != _currentUser.UserId.Value)
            {
                await AppDialogService.ShowAlertAsync(this, "Not found", "Deck not found.");
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
            _pendingTrackedStudySeconds = 0;
            _studySegmentStartedAt = _cards.Count > 0 ? DateTimeOffset.UtcNow : null;

            UpdateCardDisplay();
        }

        private void UpdateCardDisplay()
        {
            HasCards = _cards.Count > 0;
            CanShowAnswer = HasCards && !_showBack;
            CanRate = HasCards && _showBack;

            if (!HasCards)
            {
                CurrentSideText = HtmlHelper.WrapWithDarkTheme("<div style=\"min-height:100%;display:flex;align-items:center;justify-content:center;text-align:center;color:#66708A;font-style:italic;padding:24px;\">No cards due yet.</div>");
                ProgressText = string.Empty;
            }
            else
            {
                var card = _cards[_currentIndex];
                CurrentSideText = HtmlHelper.WrapWithDarkTheme(_showBack ? card.Back : card.Front);
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

            TrackStudySegment();
            _showBack = !_showBack;
            UpdateCardDisplay();
        }

        private void OnShowAnswerClicked(object? sender, EventArgs e)
        {
            if (_cards.Count == 0)
            {
                return;
            }

            TrackStudySegment();
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

            TrackStudySegment();
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
                await PersistTrackedStudyTimeAsync(force: true);
                await AppDialogService.ShowAlertAsync(this, "Done", "You've completed all due cards for now.");
                await Shell.Current.GoToAsync("//MainPage");
                return;
            }

            if (_currentIndex >= _cards.Count)
            {
                _currentIndex = 0;
            }

            _showBack = false;
            _studySegmentStartedAt = DateTimeOffset.UtcNow;
            await PersistTrackedStudyTimeAsync(force: false);
            UpdateCardDisplay();
        }

        private void TrackStudySegment()
        {
            if (_studySegmentStartedAt is null)
            {
                _studySegmentStartedAt = DateTimeOffset.UtcNow;
                return;
            }

            var elapsed = DateTimeOffset.UtcNow - _studySegmentStartedAt.Value;
            var trackedSeconds = (int)Math.Floor(Math.Min(elapsed.TotalSeconds, MaxTrackedSegmentSeconds));
            if (trackedSeconds > 0)
            {
                _pendingTrackedStudySeconds += trackedSeconds;
            }

            _studySegmentStartedAt = DateTimeOffset.UtcNow;
        }

        private async Task PersistTrackedStudyTimeAsync(bool force)
        {
            if (_pendingTrackedStudySeconds <= 0)
            {
                return;
            }

            if (!force && _pendingTrackedStudySeconds < PersistThresholdSeconds)
            {
                return;
            }

            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                _pendingTrackedStudySeconds = 0;
                return;
            }

            var profile = await _userProfileRepository.GetAsync(_currentUser.UserId.Value, CancellationToken.None);
            if (profile is null)
            {
                _pendingTrackedStudySeconds = 0;
                return;
            }

            var now = DateTimeOffset.Now;
            if (profile.StudySecondsTrackedAt?.LocalDateTime.Date != now.LocalDateTime.Date)
            {
                profile.StudySecondsToday = 0;
            }

            profile.StudySecondsToday += _pendingTrackedStudySeconds;
            profile.StudySecondsTrackedAt = now;
            await _userProfileRepository.UpsertAsync(profile, CancellationToken.None);

            _pendingTrackedStudySeconds = 0;
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
