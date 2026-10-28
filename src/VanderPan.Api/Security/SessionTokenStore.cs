using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace VanderPan.Api.Security;

public sealed record ApiSession(int UserId, string Name, string Email, string Role, DateTime ExpiresAtUtc);

public sealed class SessionTokenStore
{
    private readonly ConcurrentDictionary<string, ApiSession> _sessions = new();

    public string Create(ApiSession session)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _sessions[Hash(token)] = session;
        return token;
    }

    public bool TryGet(string token, out ApiSession session)
    {
        var key = Hash(token);
        if (_sessions.TryGetValue(key, out session!) && session.ExpiresAtUtc > DateTime.UtcNow)
        {
            return true;
        }

        _sessions.TryRemove(key, out _);
        session = null!;
        return false;
    }

    public void Revoke(string token) => _sessions.TryRemove(Hash(token), out _);

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
