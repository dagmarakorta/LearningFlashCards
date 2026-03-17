using System.Collections.ObjectModel;
using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Application.Import;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;

namespace LearningFlashCards.Maui
{
    public partial class ImportCardsPage : ContentPage
    {
        public ObservableCollection<DeckOption> Decks { get; } = new();

        private readonly IDeckRepository _deckRepository;
        private readonly ICardRepository _cardRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly CsvCardImportParser _csvParser = new();
        private readonly List<ImportedCardRow> _cards = new();
        private readonly List<int> _invalidRows = new();
        private string? _selectedFileName;

        public ImportCardsPage()
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

        private async Task LoadDecksAsync()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await AppDialogService.ShowAlertAsync(this, "Not signed in", "Please login to import cards.");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var decks = await _deckRepository.GetByOwnerAsync(_currentUser.UserId.Value, CancellationToken.None);
            Decks.Clear();

            foreach (var deck in decks.OrderBy(d => d.Name))
            {
                Decks.Add(new DeckOption(deck.Id, deck.Name));
            }

            if (Decks.Count > 0 && DeckPicker.SelectedIndex < 0)
            {
                DeckPicker.SelectedIndex = 0;
            }
            else if (Decks.Count == 0)
            {
                NewDeckSwitch.IsToggled = true;
            }
        }

        private async void OnChooseFileClicked(object? sender, EventArgs e)
        {
            var pickOptions = new PickOptions
            {
                PickerTitle = "Select a CSV file",
                FileTypes = GetCsvFileTypes()
            };

            var result = await FilePicker.Default.PickAsync(pickOptions);
            if (result is null)
            {
                return;
            }

            _selectedFileName = result.FileName;
            FileNameLabel.Text = $"Selected: {_selectedFileName}";

            await ParseCsvAsync(result);
            UpdateSummary();
        }

        private async void OnImportClicked(object? sender, EventArgs e)
        {
            if (_cards.Count == 0)
            {
                await AppDialogService.ShowAlertAsync(this, "No cards", "Select a CSV file with at least one card.");
                return;
            }

            if (_invalidRows.Count > 0)
            {
                await AppDialogService.ShowAlertAsync(this, "Invalid rows", BuildInvalidLinesMessage());
                return;
            }

            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await AppDialogService.ShowAlertAsync(this, "Not signed in", "Please login to import cards.");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            Deck? deck;
            if (NewDeckSwitch.IsToggled)
            {
                var name = NewDeckNameEntry.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    await AppDialogService.ShowAlertAsync(this, "Missing name", "Please enter a new deck name.");
                    return;
                }

                deck = new Deck
                {
                    OwnerId = _currentUser.UserId.Value,
                    Name = name,
                    Description = string.IsNullOrWhiteSpace(NewDeckDescriptionEditor.Text)
                        ? null
                        : NewDeckDescriptionEditor.Text.Trim()
                };

                await _deckRepository.UpsertAsync(deck, CancellationToken.None);
            }
            else
            {
                if (DeckPicker.SelectedItem is not DeckOption selected)
                {
                    await AppDialogService.ShowAlertAsync(this, "Missing deck", "Select a deck for the import.");
                    return;
                }

                deck = await _deckRepository.GetAsync(selected.Id, CancellationToken.None);
                if (deck is null || deck.OwnerId != _currentUser.UserId.Value)
                {
                    await AppDialogService.ShowAlertAsync(this, "Not found", "Deck not found.");
                    return;
                }
            }

            foreach (var row in _cards)
            {
                var card = new Card
                {
                    DeckId = deck.Id,
                    Front = row.Front,
                    Back = row.Back
                };

                await _cardRepository.UpsertAsync(card, CancellationToken.None);
            }

            await AppDialogService.ShowAlertAsync(this, "Imported", $"Added {_cards.Count} cards.");
            await Shell.Current.GoToAsync($"{nameof(DeckDetailPage)}?deckId={deck.Id}");
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private void OnNewDeckToggled(object? sender, ToggledEventArgs e)
        {
            NewDeckFields.IsVisible = e.Value;
            DeckPicker.IsEnabled = !e.Value;
        }

        private async Task ParseCsvAsync(FileResult result)
        {
            _cards.Clear();
            _invalidRows.Clear();

            await using var stream = await result.OpenReadAsync();
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync();

            var parseResult = _csvParser.Parse(content);
            _cards.AddRange(parseResult.Cards);
            _invalidRows.AddRange(parseResult.InvalidRowNumbers);
        }

        private void UpdateSummary()
        {
            if (string.IsNullOrWhiteSpace(_selectedFileName))
            {
                SummaryLabel.Text = "Select a CSV file to preview.";
                ImportButton.IsEnabled = false;
                return;
            }

            if (_cards.Count == 0)
            {
                SummaryLabel.Text = "No valid cards found yet.";
                ImportButton.IsEnabled = false;
                return;
            }

            if (_invalidRows.Count > 0)
            {
                SummaryLabel.Text = $"Fix {_invalidRows.Count} rows missing front or back before importing.";
                ImportButton.IsEnabled = false;
                return;
            }

            SummaryLabel.Text = $"Ready to import {_cards.Count} cards.";
            ImportButton.IsEnabled = true;
        }

        private string BuildInvalidLinesMessage()
        {
            if (_invalidRows.Count == 0)
            {
                return "All rows look good.";
            }

            var preview = string.Join(", ", _invalidRows.Take(5));
            if (_invalidRows.Count > 5)
            {
                preview += ", ...";
            }

            return $"Rows missing front or back: {preview}.";
        }

        private static FilePickerFileType GetCsvFileTypes()
        {
            return new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".csv" } },
                { DevicePlatform.MacCatalyst, new[] { "csv" } },
                { DevicePlatform.macOS, new[] { "csv" } },
                { DevicePlatform.iOS, new[] { "public.comma-separated-values-text" } },
                { DevicePlatform.Android, new[] { "text/csv", "text/comma-separated-values", "application/vnd.ms-excel" } }
            });
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

        public sealed record DeckOption(Guid Id, string Name);
    }
}
