using Microsoft.Maui.Controls.Shapes;

namespace LearningFlashCards.Maui
{
    internal sealed class StyledDialogPage : ContentPage
    {
        private readonly TaskCompletionSource<bool> _resultSource = new();

        private StyledDialogPage(
            string title,
            string message,
            string acceptText,
            string? cancelText)
        {
            BackgroundColor = Colors.Transparent;
            Shell.SetNavBarIsVisible(this, false);

            var overlay = new Grid
            {
                BackgroundColor = Color.FromArgb("#660B1020"),
                Padding = new Thickness(24)
            };

            var dialogCard = new Border
            {
                MaximumWidthRequest = 460,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Padding = new Thickness(28, 24),
                StrokeThickness = 1
            };
            dialogCard.SetDynamicResource(Border.BackgroundColorProperty, "DashboardPanel");
            dialogCard.SetDynamicResource(Border.StrokeProperty, "DashboardBorder");
            dialogCard.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("#2A5E6A91")),
                Offset = new Point(0, 18),
                Radius = 28,
                Opacity = 0.95f
            };
            dialogCard.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(26) };

            var titleLabel = new Label
            {
                Text = title,
                FontSize = 22,
                HorizontalTextAlignment = TextAlignment.Start
            };
            titleLabel.SetDynamicResource(StyleProperty, "DashboardTitle");

            var messageLabel = new Label
            {
                Text = message,
                FontSize = 15,
                LineBreakMode = LineBreakMode.WordWrap
            };
            messageLabel.SetDynamicResource(StyleProperty, "DashboardBody");
            messageLabel.TextColor = Color.FromArgb("#4E5876");

            var buttonRow = new Grid
            {
                ColumnSpacing = 12,
                ColumnDefinitions = cancelText is null
                    ? [new ColumnDefinition(GridLength.Star)]
                    : [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)]
            };

            if (cancelText is not null)
            {
                var cancelButton = new Button
                {
                    Text = cancelText,
                    HorizontalOptions = LayoutOptions.Fill
                };
                cancelButton.SetDynamicResource(StyleProperty, "DashboardSecondaryButton");
                cancelButton.Clicked += async (_, _) => await CloseAsync(false);
                buttonRow.Add(cancelButton);
                Grid.SetColumn(cancelButton, 0);
            }

            var acceptButton = new Button
            {
                Text = acceptText,
                HorizontalOptions = LayoutOptions.Fill
            };
            acceptButton.SetDynamicResource(StyleProperty, "DashboardPrimaryButton");
            acceptButton.Clicked += async (_, _) => await CloseAsync(true);
            buttonRow.Add(acceptButton);
            Grid.SetColumn(acceptButton, cancelText is null ? 0 : 1);

            dialogCard.Content = new VerticalStackLayout
            {
                Spacing = 22,
                Children =
                {
                    titleLabel,
                    messageLabel,
                    buttonRow
                }
            };

            overlay.Children.Add(dialogCard);
            Content = overlay;
        }

        internal static async Task ShowAlertAsync(Page page, string title, string message, string acceptText)
        {
            var dialog = new StyledDialogPage(title, message, acceptText, null);
            await page.Navigation.PushModalAsync(dialog, false);
            await dialog._resultSource.Task;
        }

        internal static async Task<bool> ShowConfirmAsync(
            Page page,
            string title,
            string message,
            string acceptText,
            string cancelText)
        {
            var dialog = new StyledDialogPage(title, message, acceptText, cancelText);
            await page.Navigation.PushModalAsync(dialog, false);
            return await dialog._resultSource.Task;
        }

        protected override bool OnBackButtonPressed()
        {
            _ = CloseAsync(false);
            return true;
        }

        private async Task CloseAsync(bool result)
        {
            if (_resultSource.Task.IsCompleted)
            {
                return;
            }

            _resultSource.TrySetResult(result);
            await Navigation.PopModalAsync(false);
        }
    }
}
