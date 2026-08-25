using Unicord.Universal.Models.User;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class UserFlyout : AdaptiveFlyout
    {
        public UserFlyout(object param) : base(param)
        {
            InitializeComponent();
        }

        private void ViewFullProfile_Click(object sender, RoutedEventArgs e)
        {
            CloseHostFlyout();
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            CloseHostFlyout();
        }

        private void CopyUserId_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UserViewModel user)
                return;

            var package = new DataPackage();
            package.SetText(user.Id.ToString());
            Clipboard.SetContent(package);
        }
    }
}
