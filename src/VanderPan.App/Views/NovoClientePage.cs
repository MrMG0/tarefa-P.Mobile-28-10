using VanderPan.App.Services;
using VanderPan.Core.Models;

namespace VanderPan.App.Views;

public sealed class NovoClientePage : ContentPage
{
    private readonly MobileApiService _api;
    private readonly SessionService _session;
    private readonly Entry _razaoSocial = new() { Placeholder = "Razão social" };
    private readonly Entry _nomeFantasia = new() { Placeholder = "Nome fantasia" };
    private readonly Entry _cnpj = new() { Placeholder = "CNPJ" };
    private readonly Entry _email = new() { Placeholder = "E-mail", Keyboard = Keyboard.Email };
    private readonly Entry _telefone = new() { Placeholder = "Telefone", Keyboard = Keyboard.Telephone };
    private readonly Entry _cep = new() { Placeholder = "CEP" };
    private readonly Entry _rua = new() { Placeholder = "Rua" };
    private readonly Entry _numero = new() { Placeholder = "Número" };
    private readonly Entry _bairro = new() { Placeholder = "Bairro" };
    private readonly Picker _cidade = new() { Title = "Cidade" };
    private readonly Entry _sequencia = new() { Placeholder = "Sequência da visita (ex.: 15)", Keyboard = Keyboard.Numeric };
    private readonly Label _mensagem = new() { TextColor = Colors.Firebrick, IsVisible = false };
    private readonly Button _salvar = new() { Text = "Salvar cliente", BackgroundColor = Color.FromArgb("#D97706"), TextColor = Colors.White };

    public NovoClientePage()
    {
        Title = "Novo cliente";
        _api = IPlatformApplication.Current!.Services.GetRequiredService<MobileApiService>();
        _session = IPlatformApplication.Current!.Services.GetRequiredService<SessionService>();

        _salvar.Clicked += OnSalvarClicked;
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Cadastrar cliente", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    _razaoSocial, _nomeFantasia, _cnpj, _email, _telefone, _cep,
                    _rua, _numero, _bairro, _cidade, _sequencia, _mensagem, _salvar
                }
            }
        };
        CarregarCidades();
    }

    private async void CarregarCidades()
    {
        try
        {
            var cidades = await _api.GetCidadesAsync();
            _cidade.ItemsSource = cidades;
            _cidade.ItemDisplayBinding = new Binding(nameof(Cidade.Nome));
        }
        catch (HttpRequestException ex)
        {
            MostrarMensagem($"Não foi possível carregar as cidades: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            MostrarMensagem("A conexão com a API expirou ao carregar as cidades.");
        }
    }

    private async void OnSalvarClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_razaoSocial.Text) ||
            string.IsNullOrWhiteSpace(_cnpj.Text) ||
            string.IsNullOrWhiteSpace(_sequencia.Text) ||
            !int.TryParse(_sequencia.Text, out var sequencia) || sequencia <= 0)
        {
            MostrarMensagem("Informe razão social, CNPJ e uma sequência de visita válida.");
            return;
        }

        _salvar.IsEnabled = false;
        try
        {
            var cliente = new Cliente
            {
                RazaoSocial = _razaoSocial.Text.Trim(),
                NomeFantasia = TextoOpcional(_nomeFantasia.Text),
                Cnpj = _cnpj.Text.Trim(),
                Email = TextoOpcional(_email.Text),
                Telefone = TextoOpcional(_telefone.Text),
                Cep = TextoOpcional(_cep.Text),
                Rua = TextoOpcional(_rua.Text),
                Numero = TextoOpcional(_numero.Text),
                Bairro = TextoOpcional(_bairro.Text),
                IdCidade = (_cidade.SelectedItem as Cidade)?.Id,
                IdVendedor = _session.VendedorId,
                SequenciaVisita = sequencia
            };

            await _api.CadastrarClienteAsync(cliente);
            await DisplayAlertAsync("Cliente cadastrado", "O cliente foi adicionado à sua carteira.", "OK");
            await Navigation.PopAsync();
        }
        catch (HttpRequestException ex)
        {
            MostrarMensagem($"Não foi possível salvar o cliente: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            MostrarMensagem("A conexão com a API expirou ao salvar o cliente.");
        }
        finally
        {
            _salvar.IsEnabled = true;
        }
    }

    private void MostrarMensagem(string texto)
    {
        _mensagem.Text = texto;
        _mensagem.IsVisible = true;
    }

    private static string? TextoOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
