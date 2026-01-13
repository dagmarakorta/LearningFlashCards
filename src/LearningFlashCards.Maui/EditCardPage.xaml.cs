using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    [QueryProperty(nameof(DeckId), "deckId")]
    [QueryProperty(nameof(CardId), "cardId")]
    public partial class EditCardPage : ContentPage
    {
        private readonly IDeckRepository _deckRepository;
        private readonly ICardRepository _cardRepository;
        private readonly ICurrentUserService _currentUser;

        private Guid? _deckId;
        private Guid? _cardId;
        private Card? _card;

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

        public string? CardId
        {
            get => _cardId?.ToString();
            set
            {
                if (Guid.TryParse(value, out var id))
                {
                    _cardId = id;
                }
            }
        }

        public EditCardPage()
        {
            InitializeComponent();

            _deckRepository = GetRequiredService<IDeckRepository>();
            _cardRepository = GetRequiredService<ICardRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCardAsync();
        }

        private async Task LoadCardAsync()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            if (_cardId is null)
            {
                await DisplayAlertAsync("Missing card", "Unable to load card.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            _card = await _cardRepository.GetAsync(_cardId.Value, CancellationToken.None);
            if (_card is null)
            {
                await DisplayAlertAsync("Not found", "Card not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            FrontEditor.Text = _card.Front;
            BackEditor.Text = _card.Back;
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            var front = FrontEditor.Text?.Trim();
            var back = BackEditor.Text?.Trim();

            if (string.IsNullOrWhiteSpace(front) || string.IsNullOrWhiteSpace(back))
            {
                await DisplayAlertAsync("Missing text", "Please enter front and back text.", "OK");
                return;
            }

            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await DisplayAlertAsync("Not signed in", "Please login to edit cards.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            if (_deckId is null || _card is null)
            {
                await DisplayAlertAsync("Missing data", "Unable to save changes.", "OK");
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

            _card.Front = front;
            _card.Back = back;
            _card.ModifiedAt = DateTimeOffset.UtcNow;

            await _cardRepository.UpsertAsync(_card, CancellationToken.None);
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
