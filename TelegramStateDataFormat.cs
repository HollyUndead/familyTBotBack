using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace familyTBotBack;

/// <summary>
/// Keeps OIDC properties server-side and sends only a short opaque handle as
/// Telegram's state parameter. The normal OIDC correlation cookie and nonce
/// validation still run when the handler consumes the properties.
/// </summary>
internal sealed class TelegramStateDataFormat : ISecureDataFormat<AuthenticationProperties>
{
    private static readonly ConcurrentDictionary<string, Entry> Entries = new();
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    public string Protect(AuthenticationProperties data) => Protect(data, DateTimeOffset.UtcNow);

    public string Protect(AuthenticationProperties data, string? purpose) => Protect(data, DateTimeOffset.UtcNow);

    private static string Protect(AuthenticationProperties data, DateTimeOffset now)
    {
        RemoveExpired(now);

        var handle = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var snapshot = new StateSnapshot(data.RedirectUri, new Dictionary<string, string?>(data.Items));
        Entries[handle] = new Entry(snapshot, now.Add(Lifetime));
        return handle;
    }

    public AuthenticationProperties? Unprotect(string? protectedText) => Unprotect(protectedText, null);

    public AuthenticationProperties? Unprotect(string? protectedText, string? purpose)
    {
        if (string.IsNullOrWhiteSpace(protectedText) || !Entries.TryRemove(protectedText, out var entry))
            return null;

        if (entry.ExpiresAt < DateTimeOffset.UtcNow)
            return null;

        var properties = new AuthenticationProperties(entry.Snapshot.Items)
        {
            RedirectUri = entry.Snapshot.RedirectUri
        };
        return properties;
    }

    private static void RemoveExpired(DateTimeOffset now)
    {
        foreach (var pair in Entries)
        {
            if (pair.Value.ExpiresAt < now)
                Entries.TryRemove(pair.Key, out _);
        }
    }

    private sealed record Entry(StateSnapshot Snapshot, DateTimeOffset ExpiresAt);
    private sealed record StateSnapshot(string? RedirectUri, Dictionary<string, string?> Items);
}
