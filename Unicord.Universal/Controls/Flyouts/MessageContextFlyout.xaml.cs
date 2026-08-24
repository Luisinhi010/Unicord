using Unicord.Universal.Controls.Messages;
using Unicord.Universal.Extensions;
using Unicord.Universal.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class MessageContextFlyout : MenuFlyout
    {
        public MessageContextFlyout()
        {
            InitializeComponent();
        }

        private void AddReactionButton_Click(object sender, RoutedEventArgs e)
        {
            var control = Target?.FindParent<MessageControl>();
            var page = Target?.FindParent<ChannelPage>();

            if (control?.MessageViewModel != null && page != null)
            {
                page.ShowReactionPicker(control.MessageViewModel);
            }
        }
    }
}
