using System.Collections.ObjectModel;
using System.Text;
using LearningFlashCards.Core.Application.Abstractions.Repositories;
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
                await DisplayAlertAsync("Not signed in", "Please login to import cards.", "OK");
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
                await DisplayAlertAsync("No cards", "Select a CSV file with at least one card.", "OK");
                return;
            }

            if (_invalidRows.Count > 0)
            {
                await DisplayAlertAsync("Invalid rows", BuildInvalidLinesMessage(), "OK");
                return;
            }

            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                await DisplayAlertAsync("Not signed in", "Please login to import cards.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            Deck? deck;
            if (NewDeckSwitch.IsToggled)
            {
                var name = NewDeckNameEntry.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    await DisplayAlertAsync("Missing name", "Please enter a new deck name.", "OK");
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
                    await DisplayAlertAsync("Missing deck", "Select a deck for the import.", "OK");
                    return;
                }

                deck = await _deckRepository.GetAsync(selected.Id, CancellationToken.None);
                if (deck is null || deck.OwnerId != _currentUser.UserId.Value)
                {
                    await DisplayAlertAsync("Not found", "Deck not found.", "OK");
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

            await DisplayAlertAsync("Imported", $"Added {_cards.Count} cards.", "OK");
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

            var rows = ParseCsvContent(content);
            var headerChecked = false;
            var rowNumber = 0;

            foreach (var fields in rows)
            {
                if (fields.Count == 0 || fields.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                if (!headerChecked)
                {
                    headerChecked = true;
                    if (IsHeaderRow(fields))
                    {
                        continue;
                    }
                }

                rowNumber++;

                var front = fields.ElementAtOrDefault(0)?.Trim() ?? string.Empty;
                var back = fields.ElementAtOrDefault(1)?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(front) || string.IsNullOrWhiteSpace(back))
                {
                    _invalidRows.Add(rowNumber);
                    continue;
                }

                _cards.Add(new ImportedCardRow(rowNumber, front, back));
            }
        }

        private static List<List<string>> ParseCsvContent(string content)
        {
            var rows = new List<List<string>>();
            var fields = new List<string>();
            var buffer = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < content.Length; i++)
            {
                var ch = content[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < content.Length && content[i + 1] == '"')
                    {
                        buffer.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (!inQuotes)
                {
                    if (ch == ',')
                    {
                        fields.Add(buffer.ToString());
                        buffer.Clear();
                        continue;
                    }

                    if (ch == '\r' || ch == '\n')
                    {
                        if (ch == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                        {
                            i++;
                        }

                        fields.Add(buffer.ToString());
                        buffer.Clear();
                        rows.Add(fields);
                        fields = new List<string>();
                        continue;
                    }
                }

                buffer.Append(ch);
            }

            if (buffer.Length > 0 || fields.Count > 0)
            {
                fields.Add(buffer.ToString());
                rows.Add(fields);
            }

            return rows;
        }

        private static bool IsHeaderRow(IReadOnlyList<string> fields)
        {
            if (fields.Count < 2)
            {
                return false;
            }

            var front = TrimHeaderField(fields[0]);
            var back = TrimHeaderField(fields[1]);

            return front.Equals("front", StringComparison.OrdinalIgnoreCase)
                && back.Equals("back", StringComparison.OrdinalIgnoreCase);
        }

        private static string TrimHeaderField(string value)
        {
            return value.Trim().TrimStart('\uFEFF');
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

        private sealed record ImportedCardRow(int LineNumber, string Front, string Back);
    }
}
