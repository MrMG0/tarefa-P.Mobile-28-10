using VanderPan.Core.Models;

namespace VanderPan.App.Views;

public sealed class PedidoDetalhesPage : ContentPage
{
    public PedidoDetalhesPage(Pedido pedido)
    {
        Title = $"Pedido #{pedido.Id}";
        var conteudo = new VerticalStackLayout { Padding = 20, Spacing = 12 };
        conteudo.Children.Add(new Label { Text = $"Status: {pedido.Status}", FontSize = 18, FontAttributes = FontAttributes.Bold });
        conteudo.Children.Add(new Label { Text = $"Data: {pedido.DataPedido.ToLocalTime():dd/MM/yyyy HH:mm}" });
        conteudo.Children.Add(new Label { Text = $"Entrega: {pedido.JanelaEntrega}" });
        conteudo.Children.Add(new Label { Text = $"Total: R$ {pedido.ValorTotal:N2}", FontSize = 18, FontAttributes = FontAttributes.Bold });
        foreach (var item in pedido.Itens)
        {
            var quantidade = item.QuantidadeEntregue ?? item.QuantidadePedida;
            conteudo.Children.Add(new Label
            {
                Text = $"{item.Produto?.Nome ?? $"Produto #{item.IdProduto}"} — {quantidade:N3} x R$ {item.PrecoUnitario:N2} = R$ {item.Subtotal:N2}"
            });
        }
        Content = new ScrollView { Content = conteudo };
    }
}
