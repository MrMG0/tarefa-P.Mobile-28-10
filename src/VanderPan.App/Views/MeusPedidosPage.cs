using Microsoft.Maui.Controls.Shapes;
using VanderPan.App.Services;
using VanderPan.Core.Models;

namespace VanderPan.App.Views;

public sealed class MeusPedidosPage : ContentPage
{
    private readonly MobileApiService _api;
    private readonly SessionService _session;
    private readonly CollectionView _pedidos = new();

    public MeusPedidosPage()
    {
        Title = "Meus pedidos";
        var services = IPlatformApplication.Current!.Services;
        _api = services.GetRequiredService<MobileApiService>();
        _session = services.GetRequiredService<SessionService>();
        _pedidos.SelectionMode = SelectionMode.Single;
        _pedidos.SelectionChanged += async (_, args) =>
        {
            if (args.CurrentSelection.FirstOrDefault() is Pedido pedido)
            {
                _pedidos.SelectedItem = null;
                await Navigation.PushAsync(new PedidoDetalhesPage(pedido));
            }
        };
        _pedidos.ItemTemplate = new DataTemplate(() =>
        {
            var titulo = new Label { FontAttributes = FontAttributes.Bold };
            var detalhe = new Label { TextColor = Color.FromArgb("#6B7280") };
            var total = new Label { TextColor = Color.FromArgb("#6B7280") };
            titulo.SetBinding(Label.TextProperty, new Binding(nameof(Pedido.Id), stringFormat: "Pedido #{0}"));
            detalhe.SetBinding(Label.TextProperty, nameof(Pedido.Status));
            total.SetBinding(Label.TextProperty, new Binding(nameof(Pedido.ValorTotal), stringFormat: "Total: R$ {0:N2}"));
            return new Border
            {
                Padding = 12,
                Margin = new Thickness(12, 6),
                Stroke = Color.FromArgb("#E5E7EB"),
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Content = new VerticalStackLayout { Children = { titulo, detalhe, total } }
            };
        });
        _pedidos.EmptyView = new Label { Text = "Nenhum pedido registrado.", HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        Content = new RefreshView
        {
            Content = _pedidos,
            Command = new Command(async () => await CarregarAsync())
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        try
        {
            _pedidos.ItemsSource = await _api.GetMeusPedidosAsync(_session.VendedorId);
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync("Erro ao carregar pedidos", ex.Message, "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync("Erro ao carregar pedidos", "A conexão com a API expirou.", "OK");
        }
    }
}
