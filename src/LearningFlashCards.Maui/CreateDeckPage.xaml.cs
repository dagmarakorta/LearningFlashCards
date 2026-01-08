namespace LearningFlashCards.Maui
{
    public partial class CreateDeckPage : ContentPage
    {
        public CreateDeckPage()
        {
            InitializeComponent();
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
