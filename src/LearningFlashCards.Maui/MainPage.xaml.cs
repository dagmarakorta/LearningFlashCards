using System.Collections.ObjectModel;
using System.Linq;
using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    public partial class MainPage : ContentPage
    {
        private const int DailyGoalMinutesTarget = 30;
        private readonly string[] _deckIcons = ["📁", "🧠", "💻", "🌍", "🔬", "📝"];
        private readonly string[] _deckBackgrounds =
        [
            "#EAF2FF",
            "#EAF7E1",
            "#FFF2DA",
            "#F5ECFF",
            "#E7F8F5",
            "#FFE8EB"
        ];

        public ObservableCollection<DeckDashboardItem> Decks { get; } = new();
        public ObservableCollection<DeckDashboardItem> FeaturedDecks { get; } = new();

        private readonly IDeckRepository _deckRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly ICurrentUserService _currentUser;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            _deckRepository = GetRequiredService<IDeckRepository>();
            _cardRepository = GetRequiredService<ICardRepository>();
            _userProfileRepository = GetRequiredService<IUserProfileRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();
        }

        public string GreetingName { get; private set; } = "learner";
        public string HeroMessage => $"Welcome back, {GreetingName}. 👋 Ready to study?";
        public string DeckCountLabel => $"{Decks.Count} {(Decks.Count == 1 ? "set" : "sets")}";
        public int TotalDecks { get; private set; }
        public int DueTodayCount { get; private set; }
        public int ReviewStreakDays { get; private set; }
        public int MasteredCardsCount { get; private set; }
        public int ReviewTarget { get; private set; } = 10;
        public int MasteredTarget { get; private set; } = 25;
        public double ReviewProgress { get; private set; }
        public double MasteredProgress { get; private set; }
        public int DailyGoalCompletedMinutes { get; private set; }
        public bool HasDecks => Decks.Count > 0;
        public bool HasNoDecks => !HasDecks;
        public bool HasQuickPractice => QuickPracticeDeck is not null;
        public string QuickPracticeDeckName => QuickPracticeDeck?.Name ?? "No decks yet";
        public int QuickPracticeQuestionCount { get; private set; }
        public string DailyGoalStatus => DailyGoalCompletedMinutes >= DailyGoalMinutesTarget ? "Daily goal reached." : "You're on track.";
        public string DailyGoalSummary => $"{DailyGoalCompletedMinutes} min completed";
        public string KeepGoingMessage => DueTodayCount > 0 ? "Keep the streak alive" : "Add a new deck";
        public string DeckPanelFooter => HasDecks ? "Choose your next deck" : "Create or import your first deck";
        public string ReviewProgressLabel => $"{DueTodayCount} / {ReviewTarget}";
        public string MasteredProgressLabel => $"{MasteredCardsCount} / {MasteredTarget}";

        private DeckDashboardItem? QuickPracticeDeck { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDashboardAsync();
        }

        private async void OnNewDeckClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(CreateDeckPage));
        }

        private async void OnProfileClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ProfilePage));
        }

        private async void OnImportClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ImportCardsPage));
        }

        private async void OnBrowseAllDecksClicked(object? sender, EventArgs e)
        {
            if (Decks.Count == 0)
            {
                await Shell.Current.GoToAsync(nameof(CreateDeckPage));
                return;
            }

            await Shell.Current.GoToAsync(nameof(BrowseDecksPage));
        }

        private async void OnStartStudyingClicked(object? sender, EventArgs e)
        {
            if (QuickPracticeDeck is null)
            {
                await Shell.Current.GoToAsync(nameof(CreateDeckPage));
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(StudyDeckPage)}?deckId={QuickPracticeDeck.Id}");
        }

        private async void OnFeaturedDeckTapped(object? sender, TappedEventArgs e)
        {
            if (sender is not Border border || border.BindingContext is not DeckDashboardItem deck)
            {
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(DeckDetailPage)}?deckId={deck.Id}");
        }

        private async void OnQuickPracticeClicked(object? sender, EventArgs e)
        {
            if (QuickPracticeDeck is null)
            {
                await Shell.Current.GoToAsync(nameof(CreateDeckPage));
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(StudyDeckPage)}?deckId={QuickPracticeDeck.Id}");
        }

        private async void OnEditDeckClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.BindingContext is not DeckDashboardItem deck)
            {
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(EditDeckPage)}?deckId={deck.Id}");
        }

        private async void OnDeleteDeckClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.BindingContext is not DeckDashboardItem deck)
            {
                return;
            }

            var confirm = await AppDialogService.ShowConfirmAsync(this, "Delete deck", "Delete this deck and all its cards?", "Delete");
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
                await AppDialogService.ShowAlertAsync(this, "Not found", "Deck not found.");
                return;
            }

            var deletedAt = DateTimeOffset.UtcNow;
            await _cardRepository.SoftDeleteByDeckAsync(deck.Id, deletedAt, CancellationToken.None);
            await _deckRepository.SoftDeleteAsync(deck.Id, deletedAt, CancellationToken.None);
            await LoadDashboardAsync();
        }

        private async Task LoadDashboardAsync()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var cancellationToken = CancellationToken.None;
            var deckEntities = (await _deckRepository.GetByOwnerAsync(_currentUser.UserId.Value, cancellationToken))
                .OrderBy(deck => deck.Name)
                .ToList();

            var cardTasks = deckEntities
                .Select(async deck => new DeckLoadResult(deck, await _cardRepository.GetByDeckAsync(deck.Id, cancellationToken)))
                .ToList();

            var profileTask = _userProfileRepository.GetAsync(_currentUser.UserId.Value, cancellationToken);
            var loadedDecks = await Task.WhenAll(cardTasks);
            var profile = await profileTask;

            GreetingName = string.IsNullOrWhiteSpace(profile?.DisplayName) ? "learner" : profile.DisplayName.Trim();

            Decks.Clear();
            FeaturedDecks.Clear();

            foreach (var (deck, cards) in loadedDecks)
            {
                var dashboardDeck = BuildDeckItem(deck, cards, Decks.Count);
                Decks.Add(dashboardDeck);
            }

            foreach (var deck in Decks.Take(3))
            {
                FeaturedDecks.Add(deck);
            }

            var allCards = loadedDecks.SelectMany(result => result.Cards).ToList();
            var now = DateTimeOffset.UtcNow;
            TotalDecks = Decks.Count;
            DueTodayCount = allCards.Count(card => card.State.DueAt <= now);
            ReviewStreakDays = allCards.Count == 0 ? 0 : allCards.Max(card => card.State.Streak);
            MasteredCardsCount = allCards.Count(IsMastered);
            ReviewTarget = Math.Max(10, DueTodayCount + Math.Max(4, TotalDecks * 3));
            MasteredTarget = Math.Max(25, MasteredCardsCount + Math.Max(10, TotalDecks * 6));
            ReviewProgress = CalculateProgress(DueTodayCount, ReviewTarget);
            MasteredProgress = CalculateProgress(MasteredCardsCount, MasteredTarget);
            QuickPracticeDeck = Decks
                .OrderByDescending(deck => deck.DueCount)
                .ThenByDescending(deck => deck.CardCount)
                .FirstOrDefault();
            QuickPracticeQuestionCount = QuickPracticeDeck is null ? 0 : Math.Max(5, Math.Min(15, QuickPracticeDeck.DueCount > 0 ? QuickPracticeDeck.DueCount : QuickPracticeDeck.CardCount));
            DailyGoalCompletedMinutes = Math.Min(DailyGoalMinutesTarget, (DueTodayCount * 3) + (ReviewStreakDays * 2) + (TotalDecks * 2));

            RaiseDashboardPropertiesChanged();
        }

        private void RaiseDashboardPropertiesChanged()
        {
            OnPropertyChanged(nameof(GreetingName));
            OnPropertyChanged(nameof(HeroMessage));
            OnPropertyChanged(nameof(DeckCountLabel));
            OnPropertyChanged(nameof(TotalDecks));
            OnPropertyChanged(nameof(DueTodayCount));
            OnPropertyChanged(nameof(ReviewStreakDays));
            OnPropertyChanged(nameof(MasteredCardsCount));
            OnPropertyChanged(nameof(ReviewTarget));
            OnPropertyChanged(nameof(MasteredTarget));
            OnPropertyChanged(nameof(ReviewProgress));
            OnPropertyChanged(nameof(MasteredProgress));
            OnPropertyChanged(nameof(DailyGoalCompletedMinutes));
            OnPropertyChanged(nameof(HasDecks));
            OnPropertyChanged(nameof(HasNoDecks));
            OnPropertyChanged(nameof(HasQuickPractice));
            OnPropertyChanged(nameof(QuickPracticeDeckName));
            OnPropertyChanged(nameof(QuickPracticeQuestionCount));
            OnPropertyChanged(nameof(DailyGoalStatus));
            OnPropertyChanged(nameof(DailyGoalSummary));
            OnPropertyChanged(nameof(KeepGoingMessage));
            OnPropertyChanged(nameof(DeckPanelFooter));
            OnPropertyChanged(nameof(ReviewProgressLabel));
            OnPropertyChanged(nameof(MasteredProgressLabel));
        }

        private DeckDashboardItem BuildDeckItem(Deck deck, IReadOnlyList<Card> cards, int index)
        {
            var cardCount = cards.Count;
            var dueCount = cards.Count(card => card.State.DueAt <= DateTimeOffset.UtcNow);
            var icon = _deckIcons[index % _deckIcons.Length];
            var background = _deckBackgrounds[index % _deckBackgrounds.Length];
            var summary = string.IsNullOrWhiteSpace(deck.Description) ? "Ready for another round" : deck.Description.Trim();

            return new DeckDashboardItem(deck.Id, deck.Name, summary, icon, background, cardCount, dueCount);
        }

        private static bool IsMastered(Card card)
        {
            return card.State.IntervalDays >= 21 || card.State.Streak >= 5;
        }

        private static double CalculateProgress(int value, int target)
        {
            if (target <= 0)
            {
                return 0;
            }

            return Math.Clamp((double)value / target, 0, 1);
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

        private sealed record DeckLoadResult(Deck Deck, IReadOnlyList<Card> Cards);

        public sealed record DeckDashboardItem(
            Guid Id,
            string Name,
            string Summary,
            string Icon,
            string BackgroundColor,
            int CardCount,
            int DueCount);
    }
}
