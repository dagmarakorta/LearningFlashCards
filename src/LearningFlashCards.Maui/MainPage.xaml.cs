using System.Collections.ObjectModel;

namespace LearningFlashCards.Maui
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<DeckListItem> Decks { get; } = new();

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            Decks.Add(new DeckListItem("Spanish Basics", "32 cards - last studied 2 days ago"));
            Decks.Add(new DeckListItem("History Dates", "18 cards - new"));
            Decks.Add(new DeckListItem("Biology Terms", "25 cards - 60% mastered"));
        }

        private async void OnNewDeckClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(CreateDeckPage));
        }

        public record DeckListItem(string Name, string Summary);
    }
}
