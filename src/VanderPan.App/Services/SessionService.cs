using VanderPan.Core.DTOs;

namespace VanderPan.App.Services;

public class SessionService
{
    private const string KeyToken = "VanderPan_Token";
    private const string KeyId = "VanderPan_VendedorId";
    private const string KeyNome = "VanderPan_VendedorNome";
    private const string KeyEmail = "VanderPan_VendedorEmail";

    public int VendedorId => Preferences.Default.Get<int>(KeyId, 0);
    public string? VendedorNome => Preferences.Default.Get<string?>(KeyNome, null);
    public string? VendedorEmail => Preferences.Default.Get<string?>(KeyEmail, null);

    public async Task<string?> GetTokenAsync()
    {
        var token = await SecureStorage.Default.GetAsync(KeyToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            Preferences.Default.Remove(KeyToken);
            return token;
        }

        var legacyToken = Preferences.Default.Get<string?>(KeyToken, null);
        if (string.IsNullOrWhiteSpace(legacyToken))
        {
            return null;
        }

        await SecureStorage.Default.SetAsync(KeyToken, legacyToken);
        Preferences.Default.Remove(KeyToken);
        return legacyToken;
    }

    public async Task SaveSessionAsync(LoginResponse login)
    {
        Preferences.Default.Set(KeyId, login.IdUsuario);
        Preferences.Default.Set(KeyNome, login.Nome);
        Preferences.Default.Set(KeyEmail, login.Email);
        await SecureStorage.Default.SetAsync(KeyToken, login.Token);
        Preferences.Default.Remove(KeyToken);
    }

    public void ClearSession()
    {
        SecureStorage.Default.Remove(KeyToken);
        Preferences.Default.Remove(KeyToken);
        Preferences.Default.Remove(KeyId);
        Preferences.Default.Remove(KeyNome);
        Preferences.Default.Remove(KeyEmail);
    }
}
