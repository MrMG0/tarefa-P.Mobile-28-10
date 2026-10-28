using VanderPan.App.Services;
using VanderPan.Core.DTOs;

namespace VanderPan.App.Views;

public sealed class CarrinhoPage : ContentPage
{
    private readonly CartService _cart;
    private readonly SessionService _session;
    private readonly MobileApiService _api;
    private readonly Label _total = new() { FontSize = 20, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End };
    private readonly VerticalStackLayout _itens = new() { Spacing = 8 };
    private readonly Entry _observacao = new() { Placeholder = "Observação (opcional)" };
    private readonly Button _confirmar = new() { Text = "Confirmar pedido", BackgroundColor = Color.FromArgb("#059669"), TextColor = Colors.White };

    public CarrinhoPage()
    {
        Title = "Carrinho";
        var services = IPlatformApplication.Current!.Services;
        _cart = services.GetRequiredService<CartService>();
        _session = services.GetRequiredService<SessionService>();
        _api = services.GetRequiredService<MobileApiService>();
        _confirmar.Clicked += OnConfirmarClicked;
        var layout = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) }
        };
        layout.Add(new ScrollView { Content = _itens }, 0, 0);
        layout.Add(new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 10,
            Children = { _observacao, _total, _confirmar }
        }, 0, 1);
        Content = layout;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _itens.Children.Clear();
        foreach (var item in _cart.Items)
        {
            _itens.Children.Add(new Label
            {
                Text = $"{item.Produto.Nome} — {item.Quantidade:N3} x R$ {item.Produto.Preco:N2} = R$ {item.Subtotal:N2}",
                Margin = new Thickness(12, 10)
            });
        }
        _total.Text = $"Total: R$ {_cart.Total:N2}";
        _confirmar.IsEnabled = _cart.Items.Count > 0;
    }

    private async void OnConfirmarClicked(object? sender, EventArgs e)
    {
        if (_cart.ClienteAtual == null || _cart.Items.Count == 0)
        {
            await DisplayAlertAsync("Carrinho vazio", "Selecione um cliente e pelo menos um produto.", "OK");
            return;
        }

        _confirmar.IsEnabled = false;
        try
        {
            var dto = new CriarPedidoDTO
            {
                IdVendedor = _session.VendedorId,
                IdCliente = _cart.ClienteAtual.Id,
                Observacao = _observacao.Text,
                Itens = _cart.Items.Select(i => new ItemPedidoInputDTO
                {
                    IdProduto = i.Produto.Id,
                    Quantidade = i.Quantidade
                }).ToList()
            };

            var pedido = await _api.CriarPedidoAsync(dto);
            _cart.Clear();
            await DisplayAlertAsync("Pedido registrado", $"Pedido #{pedido.Id} enviado para a distribuidora.", "OK");
            await Navigation.PopToRootAsync();
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync("Erro ao registrar pedido", ex.Message, "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync("Erro ao registrar pedido", "A conexão com a API expirou.", "OK");
        }
        finally
        {
            _confirmar.IsEnabled = true;
        }
    }
}
