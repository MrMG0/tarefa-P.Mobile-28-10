using VanderPan.App.Services;
using VanderPan.Core.Models;
using VanderPan.Core.Services;

namespace VanderPan.App.Views;

public partial class RoteiroPage : ContentPage
{
    private readonly MobileApiService _api;
    private readonly SessionService _session;
    private List<Cliente> _clientes = new();

    public RoteiroPage()
    {
        InitializeComponent();
        _api = IPlatformApplication.Current?.Services.GetService<MobileApiService>() ?? new MobileApiService();
        _session = IPlatformApplication.Current?.Services.GetService<SessionService>() ?? new SessionService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        VendedorNomeLabel.Text = $"Olá, {_session.VendedorNome ?? "Vendedor"}!";
        await CarregarRoteiro();
    }

    private async Task CarregarRoteiro()
    {
        RoteiroRefresh.IsRefreshing = true;
        try
        {
            _clientes = await _api.GetCarteiraAsync(_session.VendedorId);
            ClientesCollection.ItemsSource = _clientes;
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync("Erro ao carregar roteiro", ex.Message, "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync("Erro ao carregar roteiro", "A conexão com a API expirou.", "OK");
        }
        finally
        {
            RoteiroRefresh.IsRefreshing = false;
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await CarregarRoteiro();
    }

    private async void OnClienteTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: Cliente cliente })
        {
            await Navigation.PushAsync(new ClienteDetalhesPage(cliente));
        }
    }

    private async void OnNovoClienteClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new NovoClientePage());
    }

    private async void OnMeusPedidosClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MeusPedidosPage());
    }

    private async void OnDispararPedidosClicked(object sender, EventArgs e)
    {
        List<Pedido> pedidos;
        try
        {
            pedidos = await _api.GetMeusPedidosAsync(_session.VendedorId);
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync("Erro ao consultar pedidos", ex.Message, "OK");
            return;
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync("Erro ao consultar pedidos", "A conexão com a API expirou.", "OK");
            return;
        }
        var pendentes = pedidos.Where(p => p.Status == Core.Enums.StatusPedido.PENDENTE).ToList();

        if (!pendentes.Any())
        {
            await DisplayAlertAsync("Envio de Pedidos", "Você não possui pedidos pendentes para disparar.", "OK");
            return;
        }

        var agora = DateTime.UtcNow;
        var janela = EntregaService.AlocarJanela(agora);
        var janelaStr = EntregaService.FormatarJanela(janela);

        bool confirmar = await DisplayAlertAsync(
            "Disparar Pedidos", 
            $"Deseja disparar {pendentes.Count} pedido(s) pendente(s)?\nEles serão alocados na entrega das {janelaStr} (mais próxima do horário atual).", 
            "Sim, Disparar", 
            "Cancelar");

        if (confirmar)
        {
            var ids = pendentes.Select(p => p.Id).ToList();
            try
            {
                await _api.DispararEnvioPedidosAsync(ids);
                await DisplayAlertAsync("Sucesso", $"Pedidos disparados com sucesso para a entrega das {janelaStr}!", "OK");
            }
            catch (HttpRequestException ex)
            {
                await DisplayAlertAsync("Erro ao disparar pedidos", ex.Message, "OK");
            }
            catch (TaskCanceledException)
            {
                await DisplayAlertAsync("Erro ao disparar pedidos", "A conexão com a API expirou.", "OK");
            }
        }

    }

    private async void OnSairClicked(object sender, EventArgs e)
    {
        try
        {
            await _api.LogoutAsync();
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync("Erro ao encerrar sessão", ex.Message, "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync("Erro ao encerrar sessão", "A conexão com a API expirou.", "OK");
        }
        finally
        {
            _session.ClearSession();
            _api.SetToken(null);
        }
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
