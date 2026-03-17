namespace LearningFlashCards.Maui
{
    internal static class AppDialogService
    {
        internal static Task ShowAlertAsync(Page page, string title, string message, string acceptText = "OK")
        {
            return StyledDialogPage.ShowAlertAsync(page, title, message, acceptText);
        }

        internal static Task<bool> ShowConfirmAsync(
            Page page,
            string title,
            string message,
            string acceptText,
            string cancelText = "Cancel")
        {
            return StyledDialogPage.ShowConfirmAsync(page, title, message, acceptText, cancelText);
        }
    }
}
