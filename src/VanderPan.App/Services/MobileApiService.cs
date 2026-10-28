using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VanderPan.Core.DTOs;
using VanderPan.Core.Models;

namespace VanderPan.App.Services;

public class MobileApiService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;

    public MobileApiService()
    {
        // No Android Emulator, 10.0.2.2 mapeia para o localhost da máquina host
        var baseUrl = DeviceInfo.Platform == DevicePlatform.Android 
            ? "http://10.0.2.2:5000/" 
            : "http://localhost:5000/";

        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<LoginResponse> LoginVendedorAsync(string email, string senha)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(
                "api/auth/vendedor/login",
                new LoginRequest { Email = email, Senha = senha },
                _jsonOptions);
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
            if (response.IsSuccessStatusCode && result != null)
            {
                return result;
            }
            return new LoginResponse
            {
                Sucesso = false,
                Mensagem = result?.Mensagem ??
                    (response.StatusCode == HttpStatusCode.Unauthorized
                        ? "E-mail ou senha inválidos."
                        : $"A API recusou o login ({(int)response.StatusCode}).")
            };
        }
        catch (HttpRequestException ex)
        {
            return new LoginResponse { Sucesso = false, Mensagem = $"Falha de conexão com a API: {ex.Message}" };
        }
        catch (TaskCanceledException)
        {
            return new LoginResponse { Sucesso = false, Mensagem = "A conexão com a API expirou." };
        }
    }

    public void SetToken(string? token)
    {
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task LogoutAsync()
    {
        using var response = await _http.PostAsync("api/auth/logout", null);
        await EnsureSuccessAsync(response);
    }

    public Task<List<Cliente>> GetCarteiraAsync(int idVendedor) =>
        GetListAsync<Cliente>($"api/vendedores/{idVendedor}/carteira");

    public async Task<ClienteDebitosDTO> GetDebitosClienteAsync(int idCliente) =>
        await _http.GetFromJsonAsync<ClienteDebitosDTO>($"api/clientes/{idCliente}/debitos", _jsonOptions)
        ?? throw new InvalidDataException("A API retornou uma resposta vazia para os débitos do cliente.");

    public Task<List<ItemUltimaCompraDTO>> GetHistoricoClienteAsync(int idCliente) =>
        GetListAsync<ItemUltimaCompraDTO>($"api/clientes/{idCliente}/historico");

    public async Task CadastrarClienteAsync(Cliente cliente)
    {
        using var response = await _http.PostAsJsonAsync("api/clientes", cliente, _jsonOptions);
        await EnsureSuccessAsync(response);
    }

    public Task<List<Produto>> GetProdutosAsync(int? idCategoria = null)
    {
        var url = "api/produtos?apenasDisponiveis=true";
        if (idCategoria.HasValue) url += $"&idCategoria={idCategoria.Value}";
        return GetListAsync<Produto>(url);
    }

    public Task<List<Categoria>> GetCategoriasAsync() =>
        GetListAsync<Categoria>("api/categorias");

    public Task<List<Cidade>> GetCidadesAsync() =>
        GetListAsync<Cidade>("api/cidades");

    public async Task<Pedido> CriarPedidoAsync(CriarPedidoDTO dto)
    {
        using var response = await _http.PostAsJsonAsync("api/pedidos", dto, _jsonOptions);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<Pedido>(_jsonOptions)
            ?? throw new InvalidDataException("A API não retornou o pedido criado.");
    }

    public Task<List<Pedido>> GetMeusPedidosAsync(int idVendedor) =>
        GetListAsync<Pedido>($"api/pedidos/vendedor/{idVendedor}");

    public async Task DispararEnvioPedidosAsync(List<int> idsPedidos)
    {
        using var response = await _http.PostAsJsonAsync("api/pedidos/enviar", idsPedidos, _jsonOptions);
        await EnsureSuccessAsync(response);
    }

    private async Task<List<T>> GetListAsync<T>(string requestUri) =>
        await _http.GetFromJsonAsync<List<T>>(requestUri, _jsonOptions)
        ?? throw new InvalidDataException($"A API retornou uma resposta vazia para {requestUri}.");

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var detail = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            string.IsNullOrWhiteSpace(detail)
                ? $"A API retornou {(int)response.StatusCode} ({response.ReasonPhrase})."
                : detail,
            null,
            response.StatusCode);
    }
}
