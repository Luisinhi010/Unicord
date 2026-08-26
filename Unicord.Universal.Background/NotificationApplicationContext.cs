using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Unicord.Universal.Shared;
using Windows.ApplicationModel;
using Windows.Win32.Foundation;
using static Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE;
using static Windows.Win32.PInvoke;

namespace Unicord.Universal.Background
{
    class NotificationApplicationContext : ApplicationContext
    {
        private DiscordClient _discord = null;
        private BadgeManager _badgeManager = null;
        private TileManager _tileManager;
        private SecondaryTileManager _secondaryTileManager;
        private ToastManager _toastManager = null;

        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenu _contextMenu;
        private readonly MenuItem _openMenuItem;
        private readonly MenuItem _closeMenuItem;

        private Task _connectTask;
        private string _token = null;

        public bool CanRun { get; private set; }

        private static readonly FieldInfo _windowField
            = typeof(NotifyIcon).GetField("window", BindingFlags.NonPublic | BindingFlags.Instance);

        public NotificationApplicationContext()
        {
            Application.ApplicationExit += OnApplicationExit;

            // Never read the legacy plaintext LocalSettings token. The full-trust
            // process is packaged with Unicord and uses the same Credential Locker
            // resource as the UWP foreground process. If that access is unavailable,
            // fail closed and simply do not start background Discord connectivity.
            CredentialStore.DeleteLegacyPlaintextToken();
            if (!CredentialStore.TryGetToken(out _token))
                return;

            CanRun = true;
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = Properties.Resources.TrayIcon;
            _notifyIcon.Text = "Unicord";
            _notifyIcon.DoubleClick += OnOpenMenuItemClicked;

            _contextMenu = new ContextMenu();

            _openMenuItem = new MenuItem("Open Unicord");
            _openMenuItem.Click += OnOpenMenuItemClicked;
            _contextMenu.MenuItems.Add(_openMenuItem);

            _contextMenu.MenuItems.Add("-");

            _closeMenuItem = new MenuItem("Close");
            _closeMenuItem.Click += OnCloseMenuItemClicked;
            _contextMenu.MenuItems.Add(_closeMenuItem);

            _notifyIcon.ContextMenu = _contextMenu;
            _notifyIcon.Visible = true;

            EnableDarkMode(_notifyIcon);

            _connectTask = Task.Run(async () => await InitialiseAsync());
        }

        // here be dragons and awful hacks
        private void EnableDarkMode(NotifyIcon notifyIcon)
        {
            var osVersion = Environment.OSVersion.Version;

            if (osVersion.Major < 10 || osVersion.Build < 17763)
                return;

            try
            {
                var hwnd = new HWND(((NativeWindow)_windowField.GetValue(notifyIcon)).Handle);
                if (osVersion.Build < 18362)
                {
                    UxThemePrivate.AllowDarkModeForWindow(hwnd, true);
                }
                else
                {
                    UxThemePrivate.SetPreferredAppMode(UxThemePrivate.PreferredAppMode.AllowDark);
                }

                UxThemePrivate.FlushMenuThemes();
            }
            catch
            {
                // Cosmetic only.
            }
        }

        private async void OnOpenMenuItemClicked(object sender, EventArgs e)
        {
            var appListEntries = await Package.Current.GetAppListEntriesAsync();
            var app = appListEntries.FirstOrDefault();
            if (app != null)
                await app.LaunchAsync();
        }

        private void OnCloseMenuItemClicked(object sender, EventArgs e)
        {
            ExitThread();
        }

        private async Task InitialiseAsync()
        {
            try
            {
                _discord = new DiscordClient(new DiscordConfiguration()
                {
                    TokenType = TokenType.User,
                    Token = _token,
                    MessageCacheSize = 0,
                    ReconnectIndefinitely = true
                });

                _badgeManager = new BadgeManager(_discord);
                _tileManager = new TileManager(_discord);
                _secondaryTileManager = new SecondaryTileManager(_discord);
                _toastManager = new ToastManager();

                _discord.Ready += OnReady;
                _discord.Resumed += OnResumed;
                _discord.MessageCreated += OnDiscordMessage;
                _discord.MessageUpdated += OnMessageUpdated;
                _discord.MessageAcknowledged += OnMessageAcknowledged;

                await _discord.ConnectAsync(status: UserStatus.Invisible, idlesince: DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.GetType().FullName);
                ExitThread();
            }
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            try
            {
                if (_discord != null)
                {
                    _discord.DisconnectAsync().GetAwaiter().GetResult();
                    _discord.Dispose();
                }
            }
            catch
            {
                // Process is exiting; do not persist diagnostic details that could
                // accidentally include request/authentication context.
            }
            finally
            {
                _discord = null;
                _token = null;
            }

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }

        private async Task OnReady(DiscordClient client, ReadyEventArgs e)
        {
            await _tileManager.InitialiseAsync();
            _badgeManager.Update();
            _ = Task.Run(GCTask);
        }

        private Task OnResumed(DiscordClient sender, ResumedEventArgs args)
        {
            _ = Task.Run(GCTask);
            return Task.CompletedTask;
        }

        private async Task GCTask()
        {
            await Task.Delay(5000);
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }

        private async Task OnDiscordMessage(DiscordClient client, MessageCreateEventArgs e)
        {
            try
            {
                if (NotificationUtils.WillShowToast(client, e.Message))
                {
                    _toastManager?.HandleMessage(client, e.Message, UnicordFinder.IsUnicordVisible());
                    _badgeManager?.Update();

                    if (_tileManager != null)
                        await _tileManager.HandleMessageAsync(e.Message);
                }

                if (_secondaryTileManager != null)
                    await _secondaryTileManager.HandleMessageAsync(client, e.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        private Task OnMessageUpdated(DiscordClient client, MessageUpdateEventArgs e)
        {
            try
            {
                if (NotificationUtils.WillShowToast(client, e.Message))
                    _toastManager?.HandleMessageUpdated(client, e.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.GetType().FullName);
            }

            return Task.CompletedTask;
        }

        private async Task OnMessageAcknowledged(DiscordClient client, MessageAcknowledgeEventArgs e)
        {
            try
            {
                _badgeManager?.Update();
                _toastManager?.HandleAcknowledge(e.Channel);

                if (_tileManager != null)
                    await _tileManager.HandleAcknowledgeAsync(e.Channel);

                if (_secondaryTileManager != null)
                    await _secondaryTileManager.HandleAcknowledgeAsync(e.Channel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }
    }
}
