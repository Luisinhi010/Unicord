using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Models.Messaging;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Unicord.Universal.Services
{
    internal class DiscordManager
    {
        private static DiscordClient _discord;
        private static readonly ILogger<DiscordManager> _logger
            = Logger.GetLogger<DiscordManager>();

        private static readonly SemaphoreSlim _connectSemaphore
            = new SemaphoreSlim(1);
        private static TaskCompletionSource<ReadyEventArgs> _readySource
            = new TaskCompletionSource<ReadyEventArgs>();

        public static DiscordClient Discord => _discord;

        internal static void KickoffConnectionAsync()
        {
            CredentialStore.DeleteLegacyPlaintextToken();

            _ = Task.Run(async () =>
            {
                if (TryGetToken(out var token))
                {
                    try
                    {
                        await LoginAsync(token, null, null, true);
                    }
                    finally
                    {
                        token = null;
                    }
                }
            });
        }

        internal static async Task LoginAsync(
            string token,
            AsyncEventHandler<DiscordClient, ReadyEventArgs> onReady,
            Func<Exception, Task> onError,
            bool background,
            UserStatus status = UserStatus.Online)
        {
            await _connectSemaphore.WaitAsync();
            _readySource = new TaskCompletionSource<ReadyEventArgs>();

            if (!background)
            {
                // A foreground login is a new credential attempt. Do not leave a stale
                // credential in the vault while validating a replacement.
                CredentialStore.DeleteToken();
            }

            CredentialStore.DeleteLegacyPlaintextToken();

            try
            {
                if (Discord != null)
                {
                    try
                    {
                        var res = await _readySource.Task;
                        if (onReady != null)
                            await onReady(Discord, res);
                    }
                    catch (Exception ex)
                    {
                        if (onError != null)
                            await onError(ex);
                    }

                    return;
                }

                if (App.RoamingSettings.Read(Constants.VERIFY_LOGIN, false))
                {
                    if (background || !(await WindowsHelloManager.VerifyAsync(Constants.VERIFY_LOGIN, "VerifyLoginDisplayReason")))
                    {
                        if (onError != null)
                            await onError(null);
                        return;
                    }
                }

                try
                {
                    async Task ReadyHandler(DiscordClient sender, ReadyEventArgs e)
                    {
                        // Persist only after a successful foreground login. If Discord rotated
                        // the token before Ready, AuthTokenUpdate has already stored the newer
                        // credential, so do not overwrite it with the original value.
                        if (!background && !CredentialStore.ContainsToken())
                            CredentialStore.StoreToken(token);

                        sender.Ready -= ReadyHandler;
                        sender.SocketErrored -= SocketErrored;
                        sender.ClientErrored -= ClientErrored;
                        _readySource.TrySetResult(e);

                        if (onReady != null)
                            await onReady(sender, e);

                        onError = null;
                    }

                    Task SocketErrored(DiscordClient sender, SocketErrorEventArgs e)
                    {
                        sender.Ready -= ReadyHandler;
                        sender.SocketErrored -= SocketErrored;
                        sender.ClientErrored -= ClientErrored;

                        Logger.LogError(e.Exception);
                        _readySource.TrySetException(e.Exception);
                        return Task.CompletedTask;
                    }

                    Task ClientErrored(DiscordClient sender, ClientErrorEventArgs e)
                    {
                        sender.Ready -= ReadyHandler;
                        sender.SocketErrored -= SocketErrored;
                        sender.ClientErrored -= ClientErrored;

                        Logger.LogError(e.Exception);
                        _readySource.TrySetException(e.Exception);
                        return Task.CompletedTask;
                    }

                    _discord = new DiscordClient(new DiscordConfiguration()
                    {
                        Token = token,
                        TokenType = TokenType.User,
                        LoggerFactory = Logger.LoggerFactory,
                        ReconnectIndefinitely = true
                    });

                    Discord.Ready += ReadyHandler;
                    Discord.SocketErrored += SocketErrored;
                    Discord.ClientErrored += ClientErrored;
                    Discord.CaptchaRequested += OnDiscordCaptchaRequested;
                    Discord.AuthTokenUpdate += OnDiscordTokenUpdated;

                    DiscordClientMessenger.Register(Discord);

                    await Discord.ConnectAsync(
                        status: status,
                        idlesince: SystemPlatform.Desktop ? null : DateTimeOffset.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure when logging in!");
                    CredentialStore.DeleteToken();
                    _readySource.TrySetException(ex);

                    if (onError != null)
                        await onError(ex);
                }
            }
            finally
            {
                _connectSemaphore.Release();
            }
        }

        private static Task OnDiscordTokenUpdated(DiscordClient sender, AuthTokenUpdatedEventArgs args)
        {
            // Discord may rotate the credential. Replace the previous protected entry
            // atomically from the app's point of view; never mirror it into LocalSettings.
            CredentialStore.StoreToken(args.Token);
            CredentialStore.DeleteLegacyPlaintextToken();
            return Task.CompletedTask;
        }

        private static async Task OnDiscordCaptchaRequested(BaseDiscordClient sender, CaptchaRequestEventArgs args)
        {
            var tcs = new TaskCompletionSource<DiscordCaptchaResponse>();

            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                var dialog = new CaptchaRequestDialog(args.Request);
                await dialog.ShowAsync();
                tcs.SetResult(dialog.CaptchaResponse);
            });

            args.SetResponse(await tcs.Task);
        }

        internal static async Task LogoutAsync()
        {
            if (Discord == null)
                return;

            var discord = _discord;
            try
            {
                DiscordClientMessenger.Unregister(discord);
                discord.AuthTokenUpdate -= OnDiscordTokenUpdated;
                discord.CaptchaRequested -= OnDiscordCaptchaRequested;
                await discord.DisconnectAsync();
                discord.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when disposing of DiscordClient!");
            }
            finally
            {
                _discord = null;
            }
        }

        internal static bool TryGetToken(out string token)
        {
            CredentialStore.DeleteLegacyPlaintextToken();
            return CredentialStore.TryGetToken(out token);
        }
    }
}
