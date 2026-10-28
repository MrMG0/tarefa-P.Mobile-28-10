using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VanderPan.Core.DTOs;
using VanderPan.Core.Enums;
using VanderPan.Core.Models;
using VanderPan.Core.Services;

namespace VanderPan.Web.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(HttpClient http)
    {
        _http = http;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    // -------------------------------------------------------------------------
    // Autenticação
    // -------------------------------------------------------------------------
    public async Task<LoginResponse> LoginAdminAsync(string email, string senha)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/admin/login", new LoginRequest { Email = email, Senha = senha }, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return (await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions))!;
            }
            return new LoginResponse { Sucesso = false, Mensagem = "E-mail ou senha inválidos." };
        }
        catch (Exception ex)
        {
            return new LoginResponse { Sucesso = false, Mensagem = $"Erro ao conectar à API: {ex.Message}" };
        }
    }

    public async Task LogoutAsync()
    {
        using var response = await _http.PostAsync("api/auth/logout", null);
    }

    // -------------------------------------------------------------------------
    // Estados & Cidades
    // -------------------------------------------------------------------------
    public async Task<List<Estado>> GetEstadosAsync()
    {
        return await _http.GetFromJsonAsync<List<Estado>>("api/estados", _jsonOptions) ?? new();
    }

    public async Task<bool> SalvarEstadoAsync(Estado estado)
    {
        HttpResponseMessage res;
        if (estado.Id == 0)
            res = await _http.PostAsJsonAsync("api/estados", estado, _jsonOptions);
        else
            res = await _http.PutAsJsonAsync($"api/estados/{estado.Id}", estado, _jsonOptions);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> ExcluirEstadoAsync(int id)
    {
        var res = await _http.DeleteAsync($"api/estados/{id}");
        return res.IsSuccessStatusCode;
    }

    public async Task<List<Cidade>> GetCidadesAsync(int? idEstado = null)
    {
        var url = idEstado.HasValue ? $"api/cidades?idEstado={idEstado.Value}" : "api/cidades";
        return await _http.GetFromJsonAsync<List<Cidade>>(url, _jsonOptions) ?? new();
    }

    public async Task<bool> SalvarCidadeAsync(Cidade cidade)
    {
        HttpResponseMessage res;
        if (cidade.Id == 0)
            res = await _http.PostAsJsonAsync("api/cidades", cidade, _jsonOptions);
        else
            res = await _http.PutAsJsonAsync($"api/cidades/{cidade.Id}", cidade, _jsonOptions);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> ExcluirCidadeAsync(int id)
    {
        var res = await _http.DeleteAsync($"api/cidades/{id}");
        return res.IsSuccessStatusCode;
    }

    // -------------------------------------------------------------------------
    // Categorias
    // -------------------------------------------------------------------------
    public async Task<List<Categoria>> GetCategoriasAsync()
    {
        return await _http.GetFromJsonAsync<List<Categoria>>("api/categorias", _jsonOptions) ?? new();
    }

    public async Task<bool> SalvarCategoriaAsync(Categoria categoria)
    {
        HttpResponseMessage res;
        if (categoria.Id == 0)
            res = await _http.PostAsJsonAsync("api/categorias", categoria, _jsonOptions);
        else
            res = await _http.PutAsJsonAsync($"api/categorias/{categoria.Id}", categoria, _jsonOptions);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> ExcluirCategoriaAsync(int id)
    {
        var res = await _http.DeleteAsync($"api/categorias/{id}");
        return res.IsSuccessStatusCode;
    }

    // -------------------------------------------------------------------------
    // Produtos
    // -------------------------------------------------------------------------
    public async Task<List<Produto>> GetProdutosAsync(int? idCategoria = null, bool apenasDisponiveis = false)
    {
        var url = $"api/produtos?apenasDisponiveis={apenasDisponiveis}";
        if (idCategoria.HasValue) url += $"&idCategoria={idCategoria.Value}";
        return await _http.GetFromJsonAsync<List<Produto>>(url, _jsonOptions) ?? new();
    }

    public async Task<Produto?> GetProdutoByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<Produto>($"api/produtos/{id}", _jsonOptions);
    }

    public async Task<bool> SalvarProdutoAsync(Produto produto)
    {
        HttpResponseMessage res;
        if (produto.Id == 0)
            res = await _http.PostAsJsonAsync("api/produtos", produto, _jsonOptions);
        else
            res = await _http.PutAsJsonAsync($"api/produtos/{produto.Id}", produto, _jsonOptions);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> ExcluirProdutoAsync(int id)
    {
        var res = await _http.DeleteAsync($"api/produtos/{id}");
        return res.IsSuccessStatusCode;
    }

    // -------------------------------------------------------------------------
    // Vendedores
    // -------------------------------------------------------------------------
    public async Task<List<Vendedor>> GetVendedoresAsync()
    {
        return await _http.GetFromJsonAsync<List<Vendedor>>("api/vendedores", _jsonOptions) ?? new();
    }

    public async Task<List<Cliente>> GetCarteiraVendedorAsync(int idVendedor)
    {
        return await _http.GetFromJsonAsync<List<Cliente>>($"api/vendedores/{idVendedor}/carteira", _jsonOptions) ?? new();
    }

    public async Task<bool> SalvarVendedorAsync(Vendedor vendedor)
    {
        var dto = new VendedorCadastroDTO
        {
            Nome = vendedor.Nome,
            Email = vendedor.Email,
            Telefone = vendedor.Telefone,
            Cpf = vendedor.Cpf,
            Senha = vendedor.SenhaHash,
            Ativo = vendedor.Ativo
        };
        HttpResponseMessage res;
        if (vendedor.Id == 0)
            res = await _http.PostAsJsonAsync("api/vendedores", dto, _jsonOptions);
        else
            res = await _http.PutAsJsonAsync($"api/vendedores/{vendedor.Id}", dto, _jsonOptions);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> ExcluirVendedorAsync(int id)
    {
        var res = await _http.DeleteAsync($"api/vendedores/{id}");
        return res.IsSuccessStatusCode;
    }

    // -------------------------------------------------------------------------
    // Clientes
    // -------------------------------------------------------------------------
    public async Task<List<Cliente>> GetClientesAsync(int? idVendedor = null, string? busca = null)
    {
        var url = "api/clientes?";
        if (idVendedor.HasValue) url += $"idVendedor={idVendedor.Value}&";
        if (!string.IsNullOrWhiteSpace(busca)) url += $"busca={Uri.EscapeDataString(busca)}&";
        return await _http.GetFromJsonAsync<List<Cliente>>(url, _jsonOptions) ?? new();
    }

    public async Task<Cliente?> GetClienteByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<Cliente>($"api/clientes/{id}", _jsonOptions);
    }

    public async Task<ClienteDebitosDTO?> GetDebitosClienteAsync(int id)
    {
        return await _http.GetFromJsonAsync<ClienteDebitosDTO>($"api/clientes/{id}/debitos", _jsonOptions);
    }

    public async Task<List<ItemUltimaCompraDTO>> GetHistoricoClienteAsync(int id)
    {
        return await _http.GetFromJsonAsync<List<ItemUltimaCompraDTO>>($"api/clientes/{id}/historico", _jsonOptions) ?? new();
    }

    public async Task<bool> SalvarClienteAsync(Cliente cliente)
    {
        HttpResponseMessage res;
        if (cliente.Id == 0)
            res = await _http.PostAsJsonAsync("api/clientes", cliente, _jsonOptions);
        else
            res = await _http.PutAsJsonAsync($"api/clientes/{cliente.Id}", cliente, _jsonOptions);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> ExcluirClienteAsync(int id)
    {
        var res = await _http.DeleteAsync($"api/clientes/{id}");
        return res.IsSuccessStatusCode;
    }

    // -------------------------------------------------------------------------
    // Condições de Pagamento
    // -------------------------------------------------------------------------
    public async Task<List<CondicaoPagamento>> GetCondicoesPagamentoAsync()
    {
        return await _http.GetFromJsonAsync<List<CondicaoPagamento>>("api/condicoes-pagamento", _jsonOptions) ?? new();
    }

    public async Task<bool> SalvarCondicaoPagamentoAsync(CondicaoPagamento condicao)
    {
        HttpResponseMessage res;
        if (condicao.Id == 0)
            res = await _http.PostAsJsonAsync("api/condicoes-pagamento", condicao, _jsonOptions);
        else
            res = await _http.PutAsJsonAsync($"api/condicoes-pagamento/{condicao.Id}", condicao, _jsonOptions);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> ExcluirCondicaoPagamentoAsync(int id)
    {
        var res = await _http.DeleteAsync($"api/condicoes-pagamento/{id}");
        return res.IsSuccessStatusCode;
    }

    // -------------------------------------------------------------------------
    // Pedidos & Corte
    // -------------------------------------------------------------------------
    public async Task<List<Pedido>> GetPedidosAsync(StatusPedido? status = null, JanelaEntrega? janela = null, int? idVendedor = null, int? idCliente = null)
    {
        var url = "api/pedidos?";
        if (status.HasValue) url += $"status={status.Value}&";
        if (janela.HasValue) url += $"janela={janela.Value}&";
        if (idVendedor.HasValue) url += $"idVendedor={idVendedor.Value}&";
        if (idCliente.HasValue) url += $"idCliente={idCliente.Value}&";

        return await _http.GetFromJsonAsync<List<Pedido>>(url, _jsonOptions) ?? new();
    }

    public async Task<Pedido?> GetPedidoByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<Pedido>($"api/pedidos/{id}", _jsonOptions);
    }

    public async Task<bool> AtualizarStatusPedidoAsync(int id, StatusPedido status)
    {
        var res = await _http.PatchAsJsonAsync($"api/pedidos/{id}/status", new AtualizarStatusPedidoDTO { Status = status }, _jsonOptions);
        return res.IsSuccessStatusCode;
    }

    public async Task<List<ItemCorteSugestaoOutput>> ObterSugestaoCorteAsync(int idProduto, JanelaEntrega? janela = null)
    {
        var url = $"api/pedidos/corte/sugestao?idProduto={idProduto}";
        if (janela.HasValue) url += $"&janela={janela.Value}";
        using var response = await _http.PostAsync(url, null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ItemCorteSugestaoOutput>>(_jsonOptions) ?? new();
    }

    public async Task<bool> AplicarCorteAsync(List<ItemCorteInputDTO> itens)
    {
        var res = await _http.PostAsJsonAsync("api/pedidos/corte", new AplicarCorteDTO { Itens = itens }, _jsonOptions);
        return res.IsSuccessStatusCode;
    }

    // -------------------------------------------------------------------------
    // Vendas
    // -------------------------------------------------------------------------
    public async Task<List<Venda>> GetVendasAsync()
    {
        return await _http.GetFromJsonAsync<List<Venda>>("api/vendas", _jsonOptions) ?? new();
    }

    public async Task<Venda?> GetVendaByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<Venda>($"api/vendas/{id}", _jsonOptions);
    }

    public async Task<Venda?> GerarVendaAsync(int idPedido, int idCondicao, string numeroNota)
    {
        var res = await _http.PostAsJsonAsync("api/vendas", new GerarVendaDTO
        {
            IdPedido = idPedido,
            IdCondicao = idCondicao,
            NumeroNota = numeroNota
        }, _jsonOptions);

        if (res.IsSuccessStatusCode)
        {
            return await res.Content.ReadFromJsonAsync<Venda>(_jsonOptions);
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Contas a Receber
    // -------------------------------------------------------------------------
    public async Task<List<ContaReceber>> GetContasReceberAsync(StatusContaReceber? status = null, int? idCliente = null)
    {
        var url = "api/contas-receber?";
        if (status.HasValue) url += $"status={status.Value}&";
        if (idCliente.HasValue) url += $"idCliente={idCliente.Value}&";

        return await _http.GetFromJsonAsync<List<ContaReceber>>(url, _jsonOptions) ?? new();
    }

    public async Task<CalculoPagamentoResultado?> CalcularLiquidacaoAsync(int idTitulo, DateTime dataPagamento)
    {
        var url = $"api/contas-receber/{idTitulo}/calcular-liquidacao?dataPagamento={dataPagamento:yyyy-MM-dd}";
        var res = await _http.PostAsync(url, null);
        if (res.IsSuccessStatusCode)
        {
            return await res.Content.ReadFromJsonAsync<CalculoPagamentoResultado>(_jsonOptions);
        }
        return null;
    }

    public async Task<bool> BaixarTituloAsync(int idTitulo, DateTime dataPagamento, FormaPagamento? formaPagamento = null)
    {
        var res = await _http.PatchAsJsonAsync($"api/contas-receber/{idTitulo}/pagar", new BaixarTituloDTO
        {
            DataPagamento = dataPagamento,
            FormaPagamento = formaPagamento
        }, _jsonOptions);

        return res.IsSuccessStatusCode;
    }
}
