using System;
using Windows.Security.Credentials;
using Windows.Storage;

namespace Unicord
{
    /// <summary>
    /// Central credential boundary for the legacy Discord user credential.
    ///
    /// Credentials are persisted only in Windows Credential Locker. The old
    /// LocalSettings "Token" value is treated as insecure legacy state and is
    /// deleted whenever this store is initialised by the foreground or full-trust
    /// background process.
    /// </summary>
    internal static class CredentialStore
    {
        private const string UserName = "Default";
        private const string LegacyPlaintextTokenKey = "Token";
        private const string PendingUserName = "Pending";

        public static bool ContainsToken()
        {
            return TryRetrieveCredential(UserName, out _) ||
                   TryRetrieveCredential(PendingUserName, out _);
        }

        public static bool TryGetToken(out string token)
        {
            if (TryRetrieveCredential(UserName, out token))
                return true;

            // A pending entry means replacement was interrupted after the new
            // credential was safely staged. It remains usable instead of leaving
            // the app logged out with no recoverable credential.
            return TryRetrieveCredential(PendingUserName, out token);
        }

        public static void StoreToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Credential cannot be empty.", nameof(token));

            var vault = new PasswordVault();

            // Stage and verify the replacement before removing the current entry.
            // If the process stops at any point, TryGetToken can recover either the
            // current credential or the staged replacement.
            RemoveCredential(vault, PendingUserName);
            vault.Add(new PasswordCredential(Constants.TOKEN_IDENTIFIER, PendingUserName, token));

            if (!TryRetrieveCredential(PendingUserName, out var stagedToken) ||
                !string.Equals(stagedToken, token, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Credential Locker did not retain the staged credential.");
            }

            stagedToken = null;
            RemoveCredential(vault, UserName);
            vault.Add(new PasswordCredential(Constants.TOKEN_IDENTIFIER, UserName, token));
            RemoveCredential(vault, PendingUserName);
        }

        public static void DeleteToken()
        {
            try
            {
                var vault = new PasswordVault();
                foreach (var credential in vault.FindAllByResource(Constants.TOKEN_IDENTIFIER))
                    vault.Remove(credential);
            }
            catch
            {
                // PasswordVault throws when the resource does not exist.
            }
        }

        public static void DeleteLegacyPlaintextToken()
        {
            try
            {
                ApplicationData.Current.LocalSettings.Values.Remove(LegacyPlaintextTokenKey);
            }
            catch
            {
                // Callers never read or fall back to the legacy plaintext value.
            }
        }

        private static bool TryRetrieveCredential(string userName, out string token)
        {
            try
            {
                var vault = new PasswordVault();
                var credential = vault.Retrieve(Constants.TOKEN_IDENTIFIER, userName);
                credential.RetrievePassword();

                token = credential.Password;
                return !string.IsNullOrWhiteSpace(token);
            }
            catch
            {
                token = null;
                return false;
            }
        }

        private static void RemoveCredential(PasswordVault vault, string userName)
        {
            try
            {
                vault.Remove(vault.Retrieve(Constants.TOKEN_IDENTIFIER, userName));
            }
            catch
            {
                // Missing entries are expected during first use and recovery.
            }
        }
    }
}
