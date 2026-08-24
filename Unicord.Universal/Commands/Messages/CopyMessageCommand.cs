using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models.Messages;
using Windows.ApplicationModel.DataTransfer;

namespace Unicord.Universal.Commands.Messages
{
    public class CopyMessageCommand : DiscordCommand<MessageViewModel>
    {
        public CopyMessageCommand(MessageViewModel viewModel) : base(viewModel)
        {
        }

        public override void Execute(object parameter)
        {
            Analytics.TrackEvent("CopyMessageCommand_Invoked");

            var package = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };

            package.SetText(viewModel.Message.Content ?? string.Empty);
            Clipboard.SetContent(package);
        }
    }
}
