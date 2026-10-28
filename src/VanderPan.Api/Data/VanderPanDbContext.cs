using Microsoft.EntityFrameworkCore;
using VanderPan.Core.Models;

namespace VanderPan.Api.Data;

public class VanderPanDbContext : DbContext
{
    public VanderPanDbContext(DbContextOptions<VanderPanDbContext> options) : base(options)
    {
    }

    public DbSet<Estado> Estados => Set<Estado>();
    public DbSet<Cidade> Cidades => Set<Cidade>();
    public DbSet<Vendedor> Vendedores => Set<Vendedor>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();
    public DbSet<CondicaoPagamento> CondicoesPagamento => Set<CondicaoPagamento>();
    public DbSet<ParcelaCondicao> ParcelasCondicao => Set<ParcelaCondicao>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();
    public DbSet<ContaReceber> ContasReceber => Set<ContaReceber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Índices únicos
        modelBuilder.Entity<Estado>()
            .HasIndex(e => e.Uf)
            .IsUnique();

        modelBuilder.Entity<Vendedor>()
            .HasIndex(v => v.Email)
            .IsUnique();

        modelBuilder.Entity<Vendedor>()
            .HasIndex(v => v.Cpf)
            .IsUnique();

        modelBuilder.Entity<Vendedor>()
            .Property(v => v.SenhaHash)
            .IsRequired();

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.Cnpj)
            .IsUnique();

        modelBuilder.Entity<Venda>()
            .HasIndex(v => v.IdPedido)
            .IsUnique();

        modelBuilder.Entity<CondicaoPagamento>()
            .HasIndex(cp => cp.Codigo)
            .IsUnique();

        // Conversões de enum para string ou int consistentes
        modelBuilder.Entity<Produto>()
            .Property(p => p.UnidadeMedida)
            .HasConversion<string>();

        modelBuilder.Entity<Pedido>()
            .Property(p => p.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Pedido>()
            .Property(p => p.JanelaEntrega)
            .HasConversion<string>();

        modelBuilder.Entity<ParcelaCondicao>()
            .Property(p => p.FormaPagamento)
            .HasConversion<string>();

        modelBuilder.Entity<ContaReceber>()
            .Property(c => c.FormaPagamento)
            .HasConversion<string>();

        modelBuilder.Entity<ContaReceber>()
            .Property(c => c.Status)
            .HasConversion<string>();

        // Precisão decimal
        modelBuilder.Entity<Produto>()
            .Property(p => p.Preco)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Produto>()
            .Property(p => p.Estoque)
            .HasPrecision(10, 3);

        modelBuilder.Entity<Pedido>()
            .Property(p => p.ValorTotal)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ItemPedido>()
            .Property(p => p.QuantidadePedida)
            .HasPrecision(10, 3);

        modelBuilder.Entity<ItemPedido>()
            .Property(p => p.QuantidadeEntregue)
            .HasPrecision(10, 3);

        modelBuilder.Entity<ItemPedido>()
            .Property(p => p.PrecoUnitario)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ItemPedido>()
            .Property(p => p.Subtotal)
            .HasPrecision(10, 2);

        modelBuilder.Entity<CondicaoPagamento>()
            .Property(p => p.MultaPercentual)
            .HasPrecision(5, 2);

        modelBuilder.Entity<CondicaoPagamento>()
            .Property(p => p.JurosDiaPercentual)
            .HasPrecision(5, 2);

        modelBuilder.Entity<CondicaoPagamento>()
            .Property(p => p.DescontoDiaPercentual)
            .HasPrecision(5, 2);

        modelBuilder.Entity<ParcelaCondicao>()
            .Property(p => p.Percentual)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Venda>()
            .Property(p => p.ValorTotal)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ItemVenda>()
            .Property(p => p.Quantidade)
            .HasPrecision(10, 3);

        modelBuilder.Entity<ItemVenda>()
            .Property(p => p.PrecoUnitario)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ItemVenda>()
            .Property(p => p.Subtotal)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ContaReceber>()
            .Property(p => p.ValorOriginal)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ContaReceber>()
            .Property(p => p.ValorMulta)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ContaReceber>()
            .Property(p => p.ValorJuros)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ContaReceber>()
            .Property(p => p.ValorDesconto)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ContaReceber>()
            .Property(p => p.ValorPago)
            .HasPrecision(10, 2);
    }
}
