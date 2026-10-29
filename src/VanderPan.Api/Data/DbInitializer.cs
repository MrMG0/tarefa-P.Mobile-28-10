using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using VanderPan.Core.Enums;
using VanderPan.Core.Models;

namespace VanderPan.Api.Data;

public static class DbInitializer
{
    private const string ProdutoCorteDemo = "Produto Demonstração - Corte de Estoque";
    private const string MarcadorPedidoCorteDemo = "DEMO-CORTE-ESTOQUE";

    public static void Initialize(VanderPanDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Estados.Any())
        {
            return; // Banco já populado
        }

        // Estados
        var sp = new Estado { Nome = "São Paulo", Uf = "SP", Ativo = true };
        var pr = new Estado { Nome = "Paraná", Uf = "PR", Ativo = true };
        context.Estados.AddRange(sp, pr);
        context.SaveChanges();

        // Cidades
        var curitiba = new Cidade { Nome = "Curitiba", IdEstado = pr.Id, Ativo = true };
        var sjp = new Cidade { Nome = "São José dos Pinhais", IdEstado = pr.Id, Ativo = true };
        var saoPaulo = new Cidade { Nome = "São Paulo", IdEstado = sp.Id, Ativo = true };
        context.Cidades.AddRange(curitiba, sjp, saoPaulo);
        context.SaveChanges();

        // Vendedores (hash bcrypt da senha padrão "123456")
        var hashSenha = BCrypt.Net.BCrypt.HashPassword("123456");
        var vendedor1 = new Vendedor
        {
            Nome = "Carlos Oliveira",
            Email = "carlos@vanderpan.com.br",
            Telefone = "(41) 98888-1111",
            Cpf = "111.222.333-44",
            SenhaHash = hashSenha,
            Ativo = true
        };
        var vendedor2 = new Vendedor
        {
            Nome = "Mariana Santos",
            Email = "mariana@vanderpan.com.br",
            Telefone = "(41) 97777-2222",
            Cpf = "222.333.444-55",
            SenhaHash = hashSenha,
            Ativo = true
        };
        context.Vendedores.AddRange(vendedor1, vendedor2);
        context.SaveChanges();

        // Clientes (com sequência de visita espaçada: 10, 20, 30...)
        var cliente1 = new Cliente
        {
            RazaoSocial = "Panificadora Pão Dourado Ltda",
            NomeFantasia = "Panificadora Pão Dourado",
            Cnpj = "12.345.678/0001-90",
            Email = "contato@paodourado.com.br",
            Telefone = "(41) 3222-1111",
            Cep = "80000-000",
            Rua = "Rua das Flores",
            Numero = "100",
            Bairro = "Centro",
            IdCidade = curitiba.Id,
            IdVendedor = vendedor1.Id,
            SequenciaVisita = 10,
            Ativo = true
        };

        var cliente2 = new Cliente
        {
            RazaoSocial = "Padaria e Confeitaria Doce Pão",
            NomeFantasia = "Doce Pão",
            Cnpj = "23.456.789/0001-01",
            Email = "docepao@gmail.com",
            Telefone = "(41) 3333-2222",
            Cep = "80000-010",
            Rua = "Av. Batel",
            Numero = "450",
            Bairro = "Batel",
            IdCidade = curitiba.Id,
            IdVendedor = vendedor1.Id,
            SequenciaVisita = 20,
            Ativo = true
        };

        var cliente3 = new Cliente
        {
            RazaoSocial = "Padaria Esperança ME",
            NomeFantasia = "Padaria Esperança",
            Cnpj = "34.567.890/0001-12",
            Email = "esperanca@padaria.com",
            Telefone = "(41) 3444-3333",
            Cep = "80000-020",
            Rua = "Rua Marechal Deodoro",
            Numero = "820",
            Bairro = "Alto da XV",
            IdCidade = curitiba.Id,
            IdVendedor = vendedor1.Id,
            SequenciaVisita = 30,
            Ativo = true
        };
        context.Clientes.AddRange(cliente1, cliente2, cliente3);
        context.SaveChanges();

        // Categorias de panificação
        var catFermentos = new Categoria { Nome = "Fermentos e Leveduras", Descricao = "Fermentos biológicos frescos, secos e químicos", Ativo = true };
        var catMisturas = new Categoria { Nome = "Misturas para Bolos e Confeitaria", Descricao = "Misturas prontas para bolos, pães de ló, brownies", Ativo = true };
        var catMargarinas = new Categoria { Nome = "Gorduras e Margarinas", Descricao = "Margarinas para folhados, bolos e cremes (baldes 20L/15kg)", Ativo = true };
        var catFarinhas = new Categoria { Nome = "Farinhas e Melhoradores", Descricao = "Farinhas especiais e melhoradores de farinha", Ativo = true };
        context.Categorias.AddRange(catFermentos, catMisturas, catMargarinas, catFarinhas);
        context.SaveChanges();

        // Produtos perecíveis de panificação
        var p1 = new Produto
        {
            Nome = "Fermento Biológico Fresco 500g",
            Descricao = "Fermento biológico prensado para panificação artesanal e industrial",
            Preco = 7.50m,
            IdCategoria = catFermentos.Id,
            Disponivel = true,
            Estoque = 250.000m,
            UnidadeMedida = UnidadeMedida.UNIDADE,
            Perecivel = true,
            Ativo = true
        };
        var p2 = new Produto
        {
            Nome = "Fermento Seco Instantâneo 500g",
            Descricao = "Fermento em pó de alta atividade fermentativa",
            Preco = 18.90m,
            IdCategoria = catFermentos.Id,
            Disponivel = true,
            Estoque = 180.000m,
            UnidadeMedida = UnidadeMedida.UNIDADE,
            Perecivel = true,
            Ativo = true
        };
        var p3 = new Produto
        {
            Nome = "Margarina para Confeitaria Balde 20L",
            Descricao = "Margarina com 80% de lipídios para massas folhadas e cremes nobres",
            Preco = 185.00m,
            IdCategoria = catMargarinas.Id,
            Disponivel = true,
            Estoque = 15.000m,
            UnidadeMedida = UnidadeMedida.BALDE,
            Perecivel = true,
            Ativo = true
        };
        var p4 = new Produto
        {
            Nome = "Mistura para Pão Francês 25kg",
            Descricao = "Mistura balanceada para pão francês crocante",
            Preco = 98.00m,
            IdCategoria = catFarinhas.Id,
            Disponivel = true,
            Estoque = 60.000m,
            UnidadeMedida = UnidadeMedida.KG,
            Perecivel = true,
            Ativo = true
        };
        var p5 = new Produto
        {
            Nome = "Mistura para Bolo de Chocolate 5kg",
            Descricao = "Mistura úmida e fofa para bolos de confeitaria",
            Preco = 42.50m,
            IdCategoria = catMisturas.Id,
            Disponivel = true,
            Estoque = 40.000m,
            UnidadeMedida = UnidadeMedida.KG,
            Perecivel = true,
            Ativo = true
        };
        var p6 = new Produto
        {
            Nome = "Mistura para Salgados Fritos e Assados 5kg",
            Descricao = "Base profissional para coxinhas, esfihas e risoles",
            Preco = 38.00m,
            IdCategoria = catMisturas.Id,
            Disponivel = true,
            Estoque = 50.000m,
            UnidadeMedida = UnidadeMedida.KG,
            Perecivel = true,
            Ativo = true
        };
        context.Produtos.AddRange(p1, p2, p3, p4, p5, p6);
        context.SaveChanges();

        // Condições de Pagamento de Referência
        // Condição 10: 7/14 dias, multa 5%, juros 2% ao dia, desconto 1% ao dia
        var cond10 = new CondicaoPagamento
        {
            Codigo = "10",
            Descricao = "7/14 dias - Boleto Bancário",
            MultaPercentual = 5.00m,
            JurosDiaPercentual = 2.00m,
            DescontoDiaPercentual = 1.00m,
            Ativo = true,
            Parcelas = new List<ParcelaCondicao>
            {
                new() { NumeroParcela = 1, DiasPrazo = 7, Percentual = 50.00m, FormaPagamento = FormaPagamento.BOLETO },
                new() { NumeroParcela = 2, DiasPrazo = 14, Percentual = 50.00m, FormaPagamento = FormaPagamento.BOLETO }
            }
        };

        // Condição 20: 7/14/21 dias - PIX e Boleto
        var cond20 = new CondicaoPagamento
        {
            Codigo = "20",
            Descricao = "7/14/21 dias - PIX e Boleto",
            MultaPercentual = 5.00m,
            JurosDiaPercentual = 2.00m,
            DescontoDiaPercentual = 1.00m,
            Ativo = true,
            Parcelas = new List<ParcelaCondicao>
            {
                new() { NumeroParcela = 1, DiasPrazo = 7, Percentual = 33.33m, FormaPagamento = FormaPagamento.PIX },
                new() { NumeroParcela = 2, DiasPrazo = 14, Percentual = 33.33m, FormaPagamento = FormaPagamento.BOLETO },
                new() { NumeroParcela = 3, DiasPrazo = 21, Percentual = 33.34m, FormaPagamento = FormaPagamento.BOLETO }
            }
        };

        context.CondicoesPagamento.AddRange(cond10, cond20);
        context.SaveChanges();

        // Criação de uma venda anterior para o Cliente 1 para demonstrar histórico de compras e contas a receber
        var pedidoExemplo = new Pedido
        {
            IdVendedor = vendedor1.Id,
            IdCliente = cliente1.Id,
            DataPedido = DateTime.UtcNow.AddDays(-10),
            Status = StatusPedido.CONFIRMADO,
            JanelaEntrega = JanelaEntrega.H12,
            Observacao = "Pedido inicial de reposição semanal",
            ValorTotal = 235.00m,
            Itens = new List<ItemPedido>
            {
                new() { IdProduto = p1.Id, QuantidadePedida = 10, QuantidadeEntregue = 10, PrecoUnitario = 7.50m, Subtotal = 75.00m },
                new() { IdProduto = p5.Id, QuantidadePedida = 2, QuantidadeEntregue = 2, PrecoUnitario = 42.50m, Subtotal = 85.00m },
                new() { IdProduto = p6.Id, QuantidadePedida = 2, QuantidadeEntregue = 2, PrecoUnitario = 37.50m, Subtotal = 75.00m }
            }
        };
        context.Pedidos.Add(pedidoExemplo);
        context.SaveChanges();

        var vendaExemplo = new Venda
        {
            IdPedido = pedidoExemplo.Id,
            IdCliente = cliente1.Id,
            IdVendedor = vendedor1.Id,
            IdCondicao = cond10.Id,
            DataVenda = DateTime.UtcNow.AddDays(-10),
            NumeroNota = "123",
            ValorTotal = 235.00m,
            Itens = new List<ItemVenda>
            {
                new() { IdProduto = p1.Id, Quantidade = 10, PrecoUnitario = 7.50m, Subtotal = 75.00m },
                new() { IdProduto = p5.Id, Quantidade = 2, PrecoUnitario = 42.50m, Subtotal = 85.00m },
                new() { IdProduto = p6.Id, Quantidade = 2, PrecoUnitario = 37.50m, Subtotal = 75.00m }
            }
        };
        context.Vendas.Add(vendaExemplo);
        context.SaveChanges();

        // 123/1 e 123/2
        var cr1 = new ContaReceber
        {
            IdVenda = vendaExemplo.Id,
            IdCliente = cliente1.Id,
            NumeroParcela = 1,
            NumeroDocumento = "123/1",
            DataVencimento = DateTime.UtcNow.AddDays(-3), // Já vencida para testar cálculo de multa e juros!
            ValorOriginal = 117.50m,
            FormaPagamento = FormaPagamento.BOLETO,
            Status = StatusContaReceber.VENCIDO
        };
        var cr2 = new ContaReceber
        {
            IdVenda = vendaExemplo.Id,
            IdCliente = cliente1.Id,
            NumeroParcela = 2,
            NumeroDocumento = "123/2",
            DataVencimento = DateTime.UtcNow.AddDays(4), // A vencer para testar desconto por antecipação!
            ValorOriginal = 117.50m,
            FormaPagamento = FormaPagamento.BOLETO,
            Status = StatusContaReceber.ABERTO
        };
        context.ContasReceber.AddRange(cr1, cr2);
        context.SaveChanges();
    }

    public static int InitializeCutDemoData(VanderPanDbContext context)
    {
        var vendedor = context.Vendedores.FirstOrDefault(v => v.Email == "carlos@vanderpan.com.br");
        var clientes = context.Clientes
            .Where(c => c.NomeFantasia == "Panificadora Pão Dourado"
                || c.NomeFantasia == "Doce Pão"
                || c.NomeFantasia == "Padaria Esperança")
            .ToDictionary(c => c.NomeFantasia!);
        var categoria = context.Categorias.FirstOrDefault(c => c.Nome == "Farinhas e Melhoradores");

        if (vendedor is null || categoria is null ||
            !clientes.ContainsKey("Panificadora Pão Dourado") ||
            !clientes.ContainsKey("Doce Pão") ||
            !clientes.ContainsKey("Padaria Esperança"))
        {
            throw new InvalidOperationException(
                "Não foi possível criar os pedidos de demonstração do corte: os dados base de desenvolvimento estão incompletos.");
        }

        var produto = context.Produtos.FirstOrDefault(p => p.Nome == ProdutoCorteDemo);
        if (produto is null)
        {
            produto = new Produto
            {
                Nome = ProdutoCorteDemo,
                Descricao = "Produto exclusivo para testar a sugestão e aplicação de corte de estoque.",
                Preco = 10.00m,
                IdCategoria = categoria.Id,
                Disponivel = true,
                Estoque = 12.000m,
                UnidadeMedida = UnidadeMedida.KG,
                Perecivel = false,
                Ativo = true
            };
            context.Produtos.Add(produto);
            context.SaveChanges();
        }

        var pedidosDemo = new[]
        {
            (Codigo: $"{MarcadorPedidoCorteDemo}-01", Cliente: "Panificadora Pão Dourado", Quantidade: 10.000m, Janela: JanelaEntrega.H12),
            (Codigo: $"{MarcadorPedidoCorteDemo}-02", Cliente: "Doce Pão", Quantidade: 8.000m, Janela: JanelaEntrega.H19),
            (Codigo: $"{MarcadorPedidoCorteDemo}-03", Cliente: "Padaria Esperança", Quantidade: 6.000m, Janela: JanelaEntrega.H12)
        };
        var adicionados = 0;

        foreach (var demo in pedidosDemo)
        {
            if (context.Pedidos.Any(p => p.Observacao == demo.Codigo))
            {
                continue;
            }

            var cliente = clientes[demo.Cliente];
            var subtotal = Math.Round(produto.Preco * demo.Quantidade, 2);
            context.Pedidos.Add(new Pedido
            {
                IdVendedor = vendedor.Id,
                IdCliente = cliente.Id,
                DataPedido = DateTime.UtcNow,
                Status = StatusPedido.PENDENTE,
                JanelaEntrega = demo.Janela,
                Observacao = demo.Codigo,
                ValorTotal = subtotal,
                Itens = new List<ItemPedido>
                {
                    new()
                    {
                        IdProduto = produto.Id,
                        QuantidadePedida = demo.Quantidade,
                        QuantidadeEntregue = demo.Quantidade,
                        PrecoUnitario = produto.Preco,
                        Subtotal = subtotal
                    }
                }
            });
            adicionados++;
        }

        if (adicionados > 0)
        {
            context.SaveChanges();
        }

        return adicionados;
    }
}
