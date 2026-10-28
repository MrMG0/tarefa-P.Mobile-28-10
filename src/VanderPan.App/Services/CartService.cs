using VanderPan.Core.Models;

namespace VanderPan.App.Services;

public class CartItem
{
    public Produto Produto { get; set; } = new();
    public decimal Quantidade { get; set; }
    public decimal Subtotal => Math.Round(Produto.Preco * Quantidade, 2);
}

public class CartService
{
    public Cliente? ClienteAtual { get; private set; }
    public List<CartItem> Items { get; } = new();
    public decimal Total => Items.Sum(i => i.Subtotal);

    public event Action? OnCartChanged;

    public void SetClient(Cliente cliente)
    {
        if (ClienteAtual?.Id != cliente.Id)
        {
            ClienteAtual = cliente;
            Items.Clear();
            Notify();
        }
    }

    public void AddOrUpdateItem(Produto produto, decimal quantidade)
    {
        if (quantidade <= 0)
        {
            RemoveItem(produto.Id);
            return;
        }

        var existing = Items.FirstOrDefault(i => i.Produto.Id == produto.Id);
        if (existing != null)
        {
            existing.Quantidade = quantidade;
        }
        else
        {
            Items.Add(new CartItem { Produto = produto, Quantidade = quantidade });
        }
        Notify();
    }

    public void RemoveItem(int produtoId)
    {
        Items.RemoveAll(i => i.Produto.Id == produtoId);
        Notify();
    }

    public void Clear()
    {
        Items.Clear();
        ClienteAtual = null;
        Notify();
    }

    private void Notify() => OnCartChanged?.Invoke();
}
