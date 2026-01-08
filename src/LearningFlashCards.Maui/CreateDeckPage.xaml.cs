using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LearningFlashCards.Maui
{
    public partial class CreateDeckPage : ContentPage
    {
        private readonly IDeckRepository _deckRepository;
        private readonly ICurrentUserService _currentUser;

        public CreateDeckPage()
        {
            InitializeComponent();

            _deckRepository = GetRequiredService<IDeckRepository>();
            _currentUser = GetRequiredService<ICurrentUserService>();
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            var name = NameEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlert("Missing name", "Please enter a deck name.", "OK");
                return;
            }

            var deck = new Deck
            {
                OwnerId = _currentUser.UserId,
                Name = name,
                Description = DescriptionEditor.Text?.Trim()
            };

            await _deckRepository.UpsertAsync(deck, CancellationToken.None);
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
