using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    [QueryProperty(nameof(DeckId), "deckId")]
    public partial class CreateCardPage : ContentPage
    {
        private readonly IDeckRepository _deckRepository;
        private readonly ICardRepository _cardRepository;
        private readonly ICurrentUserService _currentUser;

        private Guid? _deckId;

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

        public CreateCardPage()
        {
            InitializeComponent();

            _deckRepository = GetRequiredService<IDeckRepository>();
            _cardRepository = GetRequiredService<ICardRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();
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
                await DisplayAlertAsync("Not signed in", "Please login to add cards.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            if (_deckId is null)
            {
                await DisplayAlertAsync("Missing deck", "Unable to add a card without a deck.", "OK");
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

            var card = new Card
            {
                DeckId = deck.Id,
                Front = front,
                Back = back
            };

            await _cardRepository.UpsertAsync(card, CancellationToken.None);
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
