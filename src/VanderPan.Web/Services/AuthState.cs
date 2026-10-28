using System.Net.Http.Headers;
using VanderPan.Core.DTOs;

namespace VanderPan.Web.Services;

public class AuthState
{
    private readonly HttpClient _http;

    public AuthState(HttpClient http)
    {
        _http = http;
    }

    public bool IsAuthenticated { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    public event Action? OnChange;

    public void Login(LoginResponse login)
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.Token);
        IsAuthenticated = true;
        Nome = login.Nome;
        Email = login.Email;
        NotifyStateChanged();
    }

    public void Logout()
    {
        _http.DefaultRequestHeaders.Authorization = null;
        IsAuthenticated = false;
        Nome = string.Empty;
        Email = string.Empty;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
