using VanderPan.App.Services;

namespace VanderPan.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly MobileApiService _api;
    private readonly SessionService _session;

    public LoginPage()
    {
        InitializeComponent();
        _api = IPlatformApplication.Current?.Services.GetService<MobileApiService>() ?? new MobileApiService();
        _session = IPlatformApplication.Current?.Services.GetService<SessionService>() ?? new SessionService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var token = await _session.GetTokenAsync();
            _api.SetToken(token);
            if (!string.IsNullOrWhiteSpace(token))
            {
                await Shell.Current.GoToAsync(nameof(RoteiroPage));
            }
        }
        catch (Exception ex)
        {
            MensagemLabel.Text = $"Não foi possível recuperar a sessão segura: {ex.Message}";
            MensagemLabel.IsVisible = true;
        }
    }

    private async void OnEntrarClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var senha = SenhaEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            MensagemLabel.Text = "Por favor preencha e-mail e senha.";
            MensagemLabel.IsVisible = true;
            return;
        }

        EntrarButton.IsEnabled = false;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        MensagemLabel.IsVisible = false;

        var resultado = await _api.LoginVendedorAsync(email, senha);

        EntrarButton.IsEnabled = true;
        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;

        if (resultado.Sucesso)
        {
            try
            {
                await _session.SaveSessionAsync(resultado);
                _api.SetToken(resultado.Token);
                await Shell.Current.GoToAsync(nameof(RoteiroPage));
            }
            catch (Exception ex)
            {
                _api.SetToken(null);
                MensagemLabel.Text = $"Não foi possível salvar a sessão segura: {ex.Message}";
                MensagemLabel.IsVisible = true;
            }
        }
        else
        {
            MensagemLabel.Text = resultado.Mensagem;
            MensagemLabel.IsVisible = true;
        }
    }
}
