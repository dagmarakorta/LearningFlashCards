namespace LearningFlashCards.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(CreateDeckPage), typeof(CreateDeckPage));
        }
    }
}
