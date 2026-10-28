using VanderPan.Core.Enums;
using VanderPan.Core.Models;

namespace VanderPan.Core.Services;

public class CalculoPagamentoResultado
{
    public decimal ValorOriginal { get; set; }
    public decimal Multa { get; set; }
    public decimal Juros { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorFinal { get; set; }
    public int DiasDiferenca { get; set; }
    public string TipoCalculo { get; set; } = "EM_DIA"; // ATRASO, ANTECIPADO, EM_DIA
}

public static class FinanceiroService
{
    /// <summary>
    /// Calcula o valor final de uma parcela com base na data de pagamento, aplicando multa, juros ou desconto.
    /// RN09 / RF24:
    /// - Pagamento em atraso: Valor = valor_original + (valor_original * multa%) + (valor_original * juros_dia% * dias_atraso)
    /// - Pagamento antecipado: Valor = valor_original - (valor_original * desconto_dia% * dias_antecipacao)
    /// - No dia: valor_original
    /// </summary>
    public static CalculoPagamentoResultado CalcularLiquidacao(
        decimal valorOriginal,
        DateTime dataVencimento,
        DateTime dataPagamento,
        decimal multaPercentual,
        decimal jurosDiaPercentual,
        decimal descontoDiaPercentual)
    {
        var dtVenc = dataVencimento.Date;
        var dtPag = dataPagamento.Date;

        var resultado = new CalculoPagamentoResultado
        {
            ValorOriginal = Math.Round(valorOriginal, 2)
        };

        if (dtPag > dtVenc)
        {
            var diasAtraso = (dtPag - dtVenc).Days;
            resultado.DiasDiferenca = diasAtraso;
            resultado.TipoCalculo = "ATRASO";

            resultado.Multa = Math.Round(valorOriginal * (multaPercentual / 100m), 2);
            resultado.Juros = Math.Round(valorOriginal * (jurosDiaPercentual / 100m) * diasAtraso, 2);
            resultado.Desconto = 0m;
            resultado.ValorFinal = Math.Round(valorOriginal + resultado.Multa + resultado.Juros, 2);
        }
        else if (dtPag < dtVenc)
        {
            var diasAntecipacao = (dtVenc - dtPag).Days;
            resultado.DiasDiferenca = diasAntecipacao;
            resultado.TipoCalculo = "ANTECIPADO";

            resultado.Multa = 0m;
            resultado.Juros = 0m;
            var desc = Math.Round(valorOriginal * (descontoDiaPercentual / 100m) * diasAntecipacao, 2);
            // Desconto não pode exceder o valor original
            resultado.Desconto = Math.Min(desc, valorOriginal);
            resultado.ValorFinal = Math.Round(valorOriginal - resultado.Desconto, 2);
        }
        else
        {
            resultado.DiasDiferenca = 0;
            resultado.TipoCalculo = "EM_DIA";
            resultado.Multa = 0m;
            resultado.Juros = 0m;
            resultado.Desconto = 0m;
            resultado.ValorFinal = resultado.ValorOriginal;
        }

        return resultado;
    }

    /// <summary>
    /// Gera as parcelas de contas a receber a partir da venda e da condição de pagamento
    /// </summary>
    public static List<ContaReceber> GerarParcelas(Venda venda, CondicaoPagamento condicao)
    {
        var parcelas = new List<ContaReceber>();
        var total = venda.ValorTotal;

        if (condicao.Parcelas == null || !condicao.Parcelas.Any())
        {
            // Parcela única à vista por padrão
            parcelas.Add(new ContaReceber
            {
                IdVenda = venda.Id,
                IdCliente = venda.IdCliente,
                NumeroParcela = 1,
                NumeroDocumento = $"{venda.NumeroNota}/1",
                DataVencimento = venda.DataVenda.AddDays(7),
                ValorOriginal = total,
                FormaPagamento = FormaPagamento.BOLETO,
                Status = StatusContaReceber.ABERTO
            });
            return parcelas;
        }

        var ordenadas = condicao.Parcelas.OrderBy(p => p.NumeroParcela).ToList();
        decimal acumulado = 0m;

        for (int i = 0; i < ordenadas.Count; i++)
        {
            var p = ordenadas[i];
            decimal valorParcela;

            if (i == ordenadas.Count - 1)
            {
                // Ajuste de centavos na última parcela
                valorParcela = total - acumulado;
            }
            else
            {
                valorParcela = Math.Round(total * (p.Percentual / 100m), 2);
                acumulado += valorParcela;
            }

            parcelas.Add(new ContaReceber
            {
                IdVenda = venda.Id,
                IdCliente = venda.IdCliente,
                NumeroParcela = p.NumeroParcela,
                NumeroDocumento = $"{venda.NumeroNota}/{p.NumeroParcela}",
                DataVencimento = venda.DataVenda.Date.AddDays(p.DiasPrazo),
                ValorOriginal = valorParcela,
                FormaPagamento = p.FormaPagamento,
                Status = StatusContaReceber.ABERTO
            });
        }

        return parcelas;
    }
}
