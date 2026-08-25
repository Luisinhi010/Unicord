using System.Linq;
using DSharpPlus.Entities;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;

namespace Unicord.Universal.Converters
{
    public class Static
    {
        // Discord's official system / community notification accounts. IsSystem is the
        // preferred signal; the IDs are a compatibility fallback for older cached payloads
        // where the optional "system" field may be missing.
        private const ulong DiscordSystemUserId = 643945264868098049;
        private const ulong DiscordCommunityUpdatesUserId = 669627189624307712;

        public static bool NotNull(object obj)
            => obj != null;

        public static bool Not(bool b)
            => !b;

        public static bool Is(MessageViewModelState state, MessageViewModelState other)
            => state == other;

        public static bool IsNot(MessageViewModelState state, MessageViewModelState other)
            => state != other;

        public static bool IsOfficialDiscordSystemDm(DiscordChannel channel)
            => channel is DiscordDmChannel dm
               && dm.Recipients?.Any(IsOfficialDiscordSystemUser) == true;

        public static bool CanUseMessageComposer(DiscordChannel channel)
            => !IsOfficialDiscordSystemDm(channel);

        public static Visibility OfficialDiscordSafetyVisibility(DiscordChannel channel)
            => IsOfficialDiscordSystemDm(channel) ? Visibility.Visible : Visibility.Collapsed;

        public static Visibility MessageComposerVisibility(DiscordChannel channel)
            => IsOfficialDiscordSystemDm(channel) ? Visibility.Collapsed : Visibility.Visible;

        private static bool IsOfficialDiscordSystemUser(DiscordUser user)
            => user != null
               && (user.IsSystem == true
                   || user.Id == DiscordSystemUserId
                   || user.Id == DiscordCommunityUpdatesUserId);
    }
}
