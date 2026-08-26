using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class TokenDialog : ContentDialog
    {
        public TokenDialog()
        {
            InitializeComponent();
        }

        public string TakeToken()
        {
            var token = TokenTextBox.Password;
            TokenTextBox.Password = string.Empty;
            return token;
        }

        public void ClearCredential()
        {
            TokenTextBox.Password = string.Empty;
        }

        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ClearCredential();
        }
    }
}
