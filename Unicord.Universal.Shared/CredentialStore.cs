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

        public static bool ContainsToken()
        {
            try
            {
                var vault = new PasswordVault();
                _ = vault.Retrieve(Constants.TOKEN_IDENTIFIER, UserName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGetToken(out string token)
        {
            try
            {
                var vault = new PasswordVault();
                var credential = vault.Retrieve(Constants.TOKEN_IDENTIFIER, UserName);
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

        public static void StoreToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new System.ArgumentException("Credential cannot be empty.", nameof(token));

            DeleteToken();

            var vault = new PasswordVault();
            vault.Add(new PasswordCredential(Constants.TOKEN_IDENTIFIER, UserName, token));
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
                // Fail closed: callers never fall back to the legacy plaintext value.
            }
        }
    }
}
