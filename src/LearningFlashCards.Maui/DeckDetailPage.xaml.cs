using System.Collections.ObjectModel;
using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Application.Import;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;

namespace LearningFlashCards.Maui
{
    [QueryProperty(nameof(DeckId), "deckId")]
    public partial class DeckDetailPage : ContentPage
    {
        private readonly IDeckRepository _deckRepository;
        private readonly ICardRepository _cardRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly CsvCardImportParser _csvParser = new();

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
                await AppDialogService.ShowAlertAsync(this, "Missing deck", "Unable to load deck.");
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
            DeckDescription = string.IsNullOrWhiteSpace(deck.Description) ? "No description" : deck.Description;
            OnPropertyChanged(nameof(DeckName));
            OnPropertyChanged(nameof(DeckDescription));

            var cards = await _cardRepository.GetByDeckAsync(deck.Id, CancellationToken.None);
            Cards.Clear();
            foreach (var card in cards)
            {
                Cards.Add(new CardListItem(card.Id, HtmlHelper.StripHtml(card.Front), HtmlHelper.StripHtml(card.Back)));
            }
        }

        private async void OnAddCardClicked(object? sender, EventArgs e)
        {
            if (_deckId is null)
            {
                await AppDialogService.ShowAlertAsync(this, "Missing deck", "Select a deck before adding a card.");
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(CreateCardPage)}?deckId={_deckId}");
        }

        private async void OnStudyClicked(object? sender, EventArgs e)
        {
            if (_deckId is null)
            {
                await AppDialogService.ShowAlertAsync(this, "Missing deck", "Select a deck before studying.");
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(StudyDeckPage)}?deckId={_deckId}");
        }

        private async void OnImportClicked(object? sender, EventArgs e)
        {
            if (_deckId is null)
            {
                await AppDialogService.ShowAlertAsync(this, "Missing deck", "Deck not available.");
                return;
            }

            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var pickOptions = new PickOptions
            {
                PickerTitle = "Select a CSV file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".csv" } },
                    { DevicePlatform.MacCatalyst, new[] { "csv" } },
                    { DevicePlatform.macOS, new[] { "csv" } },
                    { DevicePlatform.iOS, new[] { "public.comma-separated-values-text" } },
                    { DevicePlatform.Android, new[] { "text/csv", "text/comma-separated-values", "application/vnd.ms-excel" } }
                })
            };

            var result = await FilePicker.Default.PickAsync(pickOptions);
            if (result is null)
            {
                return;
            }

            await using var stream = await result.OpenReadAsync();
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync();

            var parseResult = _csvParser.Parse(content);

            if (parseResult.Cards.Count == 0)
            {
                await AppDialogService.ShowAlertAsync(this, "No cards", "No valid cards found in the selected file.");
                return;
            }

            if (parseResult.InvalidRowNumbers.Count > 0)
            {
                var preview = string.Join(", ", parseResult.InvalidRowNumbers.Take(5));
                if (parseResult.InvalidRowNumbers.Count > 5) preview += ", ...";
                await AppDialogService.ShowAlertAsync(this, "Invalid rows", $"Rows missing front or back: {preview}. Fix the file and try again.");
                return;
            }

            var deck = await _deckRepository.GetAsync(_deckId.Value, CancellationToken.None);
            if (deck is null || deck.OwnerId != _currentUser.UserId.Value)
            {
                await AppDialogService.ShowAlertAsync(this, "Not authorized", "You do not have permission to import into this deck.");
                return;
            }

            var confirm = await AppDialogService.ShowConfirmAsync(this, "Import cards", $"Import {parseResult.Cards.Count} cards into \"{deck.Name}\"?", "Import");
            if (!confirm)
            {
                return;
            }

            var cardsToInsert = parseResult.Cards.Select(row => new Card
            {
                DeckId = deck.Id,
                Front = row.Front,
                Back = row.Back
            });

            await _cardRepository.AddRangeAsync(cardsToInsert, CancellationToken.None);

            await AppDialogService.ShowAlertAsync(this, "Imported", $"Added {parseResult.Cards.Count} cards.");
            await LoadDeckAsync();
        }

        private async void OnDeleteCardClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.BindingContext is not CardListItem cardItem)
            {
                return;
            }

            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            if (_deckId is null)
            {
                return;
            }

            var card = await _cardRepository.GetAsync(cardItem.Id, CancellationToken.None);
            if (card is null || card.DeckId != _deckId)
            {
                await AppDialogService.ShowAlertAsync(this, "Error", "Card not found in this deck.");
                return;
            }

            var deck = await _deckRepository.GetAsync(_deckId.Value, CancellationToken.None);
            if (deck is null || deck.OwnerId != _currentUser.UserId.Value)
            {
                await AppDialogService.ShowAlertAsync(this, "Not authorized", "You do not have permission to delete this card.");
                return;
            }

            var confirm = await AppDialogService.ShowConfirmAsync(this, "Delete card", "Delete this card from the deck?", "Delete");
            if (!confirm)
            {
                return;
            }

            await _cardRepository.SoftDeleteAsync(card.Id, DateTimeOffset.UtcNow, CancellationToken.None);
            Cards.Remove(cardItem);
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
