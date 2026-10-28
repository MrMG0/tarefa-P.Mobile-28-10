using Microsoft.Maui.Controls.Shapes;
using VanderPan.App.Services;
using VanderPan.Core.Models;

namespace VanderPan.App.Views;

public sealed class CatalogoProdutosPage : ContentPage
{
    private readonly MobileApiService _api;
    private readonly CartService _cart;
    private readonly Picker _categorias = new() { Title = "Todas as categorias" };
    private readonly CollectionView _produtos = new();
    private List<Categoria> _listaCategorias = new();

    public CatalogoProdutosPage()
    {
        Title = "Catálogo";
        _api = IPlatformApplication.Current!.Services.GetRequiredService<MobileApiService>();
        _cart = IPlatformApplication.Current!.Services.GetRequiredService<CartService>();
        _categorias.SelectedIndexChanged += async (_, _) => await CarregarProdutos();

        _produtos.ItemTemplate = new DataTemplate(() =>
        {
            var quantidade = new Entry
            {
                Placeholder = "Qtd.",
                Keyboard = Keyboard.Numeric,
                WidthRequest = 80,
                HorizontalTextAlignment = TextAlignment.Center
            };
            var adicionar = new Button { Text = "Adicionar", BackgroundColor = Color.FromArgb("#D97706"), TextColor = Colors.White };
            var nome = new Label { FontAttributes = FontAttributes.Bold, FontSize = 16 };
            var preco = new Label { FontSize = 13, TextColor = Color.FromArgb("#6B7280") };
            var estoque = new Label { FontSize = 12, TextColor = Color.FromArgb("#6B7280") };
            var produto = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
            produto.Add(new VerticalStackLayout { Children = { nome, preco, estoque } }, 0, 0);
            produto.Add(new VerticalStackLayout { Children = { quantidade, adicionar } }, 1, 0);
            nome.SetBinding(Label.TextProperty, nameof(Produto.Nome));
            preco.SetBinding(Label.TextProperty, new Binding(nameof(Produto.Preco), stringFormat: "R$ {0:N2}"));
            estoque.SetBinding(Label.TextProperty, new Binding(nameof(Produto.Estoque), stringFormat: "Estoque: {0:N3}"));
            adicionar.Clicked += async (_, _) =>
            {
                if (produto.BindingContext is not Produto item ||
                    !decimal.TryParse(quantidade.Text, out var qtd) || qtd <= 0)
                {
                    await DisplayAlertAsync("Quantidade inválida", "Informe uma quantidade maior que zero.", "OK");
                    return;
                }
                _cart.AddOrUpdateItem(item, qtd);
                await DisplayAlertAsync("Carrinho", $"{item.Nome} adicionado ao carrinho.", "OK");
            };
            return new Border
            {
                Stroke = Color.FromArgb("#E5E7EB"),
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Padding = 12,
                Margin = new Thickness(12, 6),
                Content = produto
            };
        });

        var continuar = new Button { Text = "Ver carrinho", BackgroundColor = Color.FromArgb("#059669"), TextColor = Colors.White };
        continuar.Clicked += async (_, _) => await Navigation.PushAsync(new CarrinhoPage());
        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
            Children =
            {
                _categorias,
                _produtos,
                continuar
            }
        };
        Grid.SetRow(_produtos, 1);
        Grid.SetRow(continuar, 2);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _listaCategorias = await _api.GetCategoriasAsync();
            _categorias.ItemsSource = _listaCategorias;
            _categorias.ItemDisplayBinding = new Binding(nameof(Categoria.Nome));
            await CarregarProdutos();
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync("Erro ao carregar catálogo", ex.Message, "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync("Erro ao carregar catálogo", "A conexão com a API expirou.", "OK");
        }
    }

    private async Task CarregarProdutos()
    {
        try
        {
            var categoria = _categorias.SelectedItem as Categoria;
            _produtos.ItemsSource = await _api.GetProdutosAsync(categoria?.Id);
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync("Erro ao carregar produtos", ex.Message, "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync("Erro ao carregar produtos", "A conexão com a API expirou.", "OK");
        }
    }
}
