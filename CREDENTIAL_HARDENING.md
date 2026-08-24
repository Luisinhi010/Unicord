# Credential hardening

This branch keeps the legacy Unicord transport functional while reducing how broadly its Discord user credential is exposed.

## Scope

This is **not** an OAuth migration and it does not make the legacy `TokenType.User` transport an officially supported Discord authentication method.

The goal is narrower: if a user already supplies a credential to this legacy client, Unicord should handle that credential with the smallest practical exposure.

## Implemented protections

- Persist the Discord credential only in Windows Credential Locker (`PasswordVault`).
- Remove all writes of the credential to `ApplicationData.LocalSettings`.
- Proactively delete the old plaintext `LocalSettings["Token"]` value.
- Make the packaged full-trust notification process read the same Credential Locker resource instead of plaintext settings.
- Fail closed for background notifications if Credential Locker access is unavailable; there is no plaintext fallback.
- Remove the debug command-line credential path from the full-trust background process.
- Remove the "load credential from .txt" login path.
- Clear the password-box UI buffer immediately after taking the submitted credential.
- Replace protected credentials when DSharpPlus reports an authentication-token rotation.
- Keep explicit logout cleanup of both Credential Locker and the historical LocalSettings key.
- Mask the ephemeral GitHub Actions signing-certificate password in workflow logs.

## Credential lifetime

.NET strings are immutable, so this code cannot reliably zero every in-memory copy after use. The branch therefore focuses on avoiding unnecessary additional copies and releasing local references promptly. The active DSharpPlus client still necessarily retains authentication material while connected.

## Background notifications

`Unicord.Universal.Background` is a packaged full-trust process in the same MSIX/AppX package. It now attempts to retrieve `Unicord_Token_New` from Windows Credential Locker through the shared `CredentialStore` implementation.

If Windows does not expose that credential to the full-trust process on a particular system, the background process exits without connecting. It must never fall back to the historical plaintext LocalSettings value.

This behavior needs runtime validation on Windows 11 after the GitHub Actions package builds successfully.

## Security boundary

The following must never be introduced in this branch:

- user credentials in LocalSettings or roaming settings;
- credential files in the package, temp directory, repository, or logs;
- command-line credential arguments;
- telemetry fields containing credentials;
- a fallback that weakens storage when Credential Locker access fails.

## Remaining work

- Runtime-test Credential Locker access from the packaged full-trust process.
- Audit DSharpPlus and application logging for accidental authorization-header/request dumping.
- Audit crash reporting and analytics metadata for authentication-related data.
- Consider disabling background connectivity entirely when the foreground app can provide equivalent notifications without expanding credential lifetime.
- Add a dedicated "forget credential" recovery action if account state becomes inconsistent.
