using System.Collections.ObjectModel;
using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<DeckListItem> Decks { get; } = new();
        private readonly IDeckRepository _deckRepository;
        private readonly ICurrentUserService _currentUser;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            _deckRepository = GetRequiredService<IDeckRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDecksAsync();
        }

        private async void OnNewDeckClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(CreateDeckPage));
        }

        public record DeckListItem(string Name, string Summary);

        private async Task LoadDecksAsync()
        {
            var decks = await _deckRepository.GetByOwnerAsync(_currentUser.UserId, CancellationToken.None);
            Decks.Clear();

            foreach (var deck in decks.OrderBy(d => d.Name))
            {
                Decks.Add(new DeckListItem(deck.Name, BuildSummary(deck)));
            }
        }

        private static string BuildSummary(Deck deck)
        {
            if (!string.IsNullOrWhiteSpace(deck.Description))
            {
                return deck.Description;
            }

            return "No description";
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
