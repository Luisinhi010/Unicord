using System;
using System.Threading.Tasks;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Extensions;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Pages
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void TokenLoginButton_Click(object sender, RoutedEventArgs e)
        {
            var mainPage = this.FindParent<MainPage>();
            mainPage?.ShowConnectingOverlay();

            var dialog = new TokenDialog();
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var token = dialog.TakeToken();
                try
                {
                    if (!string.IsNullOrWhiteSpace(token))
                        await TryLoginAsync(token);
                    else
                        mainPage?.HideConnectingOverlay();
                }
                finally
                {
                    token = null;
                }
            }
            else
            {
                dialog.ClearCredential();
                mainPage?.HideConnectingOverlay();
            }
        }

        private async Task TryLoginAsync(string token)
        {
            var mainPage = this.FindParent<MainPage>();

            try
            {
                token = token.Trim('"').Trim();

                if (string.IsNullOrWhiteSpace(token))
                    throw new ArgumentException("The credential cannot be empty.");

                mainPage.ShowConnectingOverlay();
                await DiscordManager.LoginAsync(token, null, App.LoginError, false);
                Frame.Navigate(typeof(DiscordPage));
            }
            catch (Exception ex)
            {
                await UIUtilities.ShowErrorDialogAsync("Failed to login!", ex.Message);
                mainPage.HideConnectingOverlay();
            }
            finally
            {
                token = null;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var mainPage = this.FindParent<MainPage>();
            mainPage.HideConnectingOverlay();
        }
    }
}
