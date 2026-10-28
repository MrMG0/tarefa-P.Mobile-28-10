using VanderPan.App.Services;
using VanderPan.Core.Models;

namespace VanderPan.App.Views;

public partial class ClienteDetalhesPage : ContentPage
{
    private readonly Cliente _cliente;
    private readonly MobileApiService _api;
    private readonly CartService _cart;

    public ClienteDetalhesPage(Cliente cliente)
    {
        InitializeComponent();
        _cliente = cliente;
        _api = IPlatformApplication.Current?.Services.GetService<MobileApiService>() ?? new MobileApiService();
        _cart = IPlatformApplication.Current?.Services.GetService<CartService>() ?? new CartService();

        PreencherDados();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarDadosFinanceirosEHistorico();
    }

    private void PreencherDados()
    {
        RazaoSocialLabel.Text = _cliente.RazaoSocial;
        NomeFantasiaLabel.Text = string.IsNullOrEmpty(_cliente.NomeFantasia) ? "" : $"Fantasia: {_cliente.NomeFantasia}";
        CnpjLabel.Text = $"CNPJ: {_cliente.Cnpj}";
        EnderecoLabel.Text = $"📍 {_cliente.Rua}, {_cliente.Numero} - {_cliente.Bairro} ({_cliente.Cidade?.Nome ?? ""})";
        TelefoneLabel.Text = $"📞 {_cliente.Telefone ?? "Não informado"} • {_cliente.Email ?? ""}";
    }

    private async Task CarregarDadosFinanceirosEHistorico()
    {
        try
        {
            var debitos = await _api.GetDebitosClienteAsync(_cliente.Id);
            TotalVencidoLabel.Text = $"R$ {debitos.TotalVencido:N2}";
            TotalAbertoLabel.Text = $"R$ {debitos.TotalEmAberto:N2}";

            if (debitos.TotalVencido > 0)
            {
                AlertaDebitoLabel.IsVisible = true;
            }

            var historico = await _api.GetHistoricoClienteAsync(_cliente.Id);
            HistoricoContainer.Children.Clear();

            if (historico.Count > 0)
            {
                foreach (var item in historico)
                {
                    var frameItem = new Frame
                    {
                        Padding = 10,
                        CornerRadius = 8,
                        BackgroundColor = Color.FromArgb("#F9FAFB"),
                        BorderColor = Color.FromArgb("#E5E7EB")
                    };

                    var grid = new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Auto)
                        }
                    };

                    var vsl = new VerticalStackLayout();
                    vsl.Children.Add(new Label { Text = item.NomeProduto, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1F2937") });
                    vsl.Children.Add(new Label { Text = $"Última compra em {item.DataCompra:dd/MM/yyyy}", FontSize = 11, TextColor = Color.FromArgb("#6B7280") });

                    var vslRight = new VerticalStackLayout { HorizontalOptions = LayoutOptions.End };
                    vslRight.Children.Add(new Label { Text = $"{item.Quantidade} un x R$ {item.PrecoUnitario:N2}", FontSize = 12, TextColor = Color.FromArgb("#D97706"), FontAttributes = FontAttributes.Bold });
                    vslRight.Children.Add(new Label { Text = $"Total: R$ {item.Subtotal:N2}", FontSize = 11, TextColor = Color.FromArgb("#4B5563") });

                    grid.Add(vsl, 0, 0);
                    grid.Add(vslRight, 1, 0);
                    frameItem.Content = grid;

                    HistoricoContainer.Children.Add(frameItem);
                }
            }
            else
            {
                HistoricoContainer.Children.Add(new Label { Text = "Nenhuma compra anterior registrada.", FontSize = 13, TextColor = Color.FromArgb("#9CA3AF") });
            }
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync("Erro ao carregar dados do cliente", ex.Message, "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync("Erro ao carregar dados do cliente", "A conexão com a API expirou.", "OK");
        }
    }

    private async void OnIniciarPedidoClicked(object sender, EventArgs e)
    {
        _cart.SetClient(_cliente);
        await Navigation.PushAsync(new CatalogoProdutosPage());
    }
}
