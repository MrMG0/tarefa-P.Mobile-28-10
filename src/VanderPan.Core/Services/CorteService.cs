namespace VanderPan.Core.Services;

public class ItemCorteSugestaoInput
{
    public int IdItemPedido { get; set; }
    public int IdPedido { get; set; }
    public int IdCliente { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public decimal QuantidadePedida { get; set; }
    public bool PossuiDividaVencida { get; set; }
    public decimal ValorDividaVencida { get; set; }
}

public class ItemCorteSugestaoOutput
{
    public int IdItemPedido { get; set; }
    public int IdPedido { get; set; }
    public int IdCliente { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public decimal QuantidadePedida { get; set; }
    public decimal QuantidadeSugerida { get; set; }
    public bool PossuiDividaVencida { get; set; }
    public decimal ValorDividaVencida { get; set; }
    public decimal PercentualAtendimento => QuantidadePedida > 0 
        ? Math.Round((QuantidadeSugerida / QuantidadePedida) * 100m, 1) 
        : 0;
}

public static class CorteService
{
    /// <summary>
    /// Sugere o corte proporcional de estoque quando a demanda excede o estoque disponível.
    /// Prioriza clientes sem dívidas vencidas e com bom histórico de adimplência.
    /// </summary>
    public static List<ItemCorteSugestaoOutput> SugerirCorte(
        List<ItemCorteSugestaoInput> itens,
        decimal estoqueDisponivel)
    {
        var resultados = new List<ItemCorteSugestaoOutput>();
        var totalPedido = itens.Sum(i => i.QuantidadePedida);

        // Se o estoque atende a demanda integral, entrega tudo
        if (estoqueDisponivel >= totalPedido)
        {
            foreach (var item in itens)
            {
                resultados.Add(new ItemCorteSugestaoOutput
                {
                    IdItemPedido = item.IdItemPedido,
                    IdPedido = item.IdPedido,
                    IdCliente = item.IdCliente,
                    NomeCliente = item.NomeCliente,
                    QuantidadePedida = item.QuantidadePedida,
                    QuantidadeSugerida = item.QuantidadePedida,
                    PossuiDividaVencida = item.PossuiDividaVencida,
                    ValorDividaVencida = item.ValorDividaVencida
                });
            }
            return resultados;
        }

        // Separação em dois grupos:
        // Grupo A: Clientes sem dívida vencida (peso/prioridade maior)
        // Grupo B: Clientes com dívida vencida
        var grupoAdimplente = itens.Where(i => !i.PossuiDividaVencida).ToList();
        var grupoInadimplente = itens.Where(i => i.PossuiDividaVencida).ToList();

        decimal estoqueRestante = estoqueDisponivel;

        // Peso relativo: se houver clientes adimplentes e inadimplentes,
        // aloca proporcionalmente com fator de prioridade (ex: adimplente tem peso 1.5x)
        // ou primeiro atende proporcionalmente melhor quem paga em dia.
        decimal totalAdimplente = grupoAdimplente.Sum(i => i.QuantidadePedida);
        decimal totalInadimplente = grupoInadimplente.Sum(i => i.QuantidadePedida);

        decimal estoqueParaAdimplentes;
        decimal estoqueParaInadimplentes;

        if (grupoInadimplente.Any() && grupoAdimplente.Any())
        {
            // Reserva 75% da proporção para quem está em dia
            decimal proporcaoAdimplente = (totalAdimplente * 1.5m) / ((totalAdimplente * 1.5m) + totalInadimplente);
            estoqueParaAdimplentes = Math.Min(totalAdimplente, Math.Round(estoqueDisponivel * proporcaoAdimplente, 3));
            estoqueParaInadimplentes = estoqueDisponivel - estoqueParaAdimplentes;
        }
        else if (grupoAdimplente.Any())
        {
            estoqueParaAdimplentes = estoqueDisponivel;
            estoqueParaInadimplentes = 0m;
        }
        else
        {
            estoqueParaAdimplentes = 0m;
            estoqueParaInadimplentes = estoqueDisponivel;
        }

        // Distribui para adimplentes
        decimal entregueA = 0m;
        for (int idx = 0; idx < grupoAdimplente.Count; idx++)
        {
            var item = grupoAdimplente[idx];
            decimal sugerido;

            if (idx == grupoAdimplente.Count - 1)
            {
                sugerido = Math.Min(item.QuantidadePedida, estoqueParaAdimplentes - entregueA);
            }
            else
            {
                var fatia = totalAdimplente > 0 ? (item.QuantidadePedida / totalAdimplente) : 0;
                sugerido = Math.Min(item.QuantidadePedida, TruncarTresCasas(estoqueParaAdimplentes * fatia));
                entregueA += sugerido;
            }

            if (sugerido < 0) sugerido = 0;

            resultados.Add(new ItemCorteSugestaoOutput
            {
                IdItemPedido = item.IdItemPedido,
                IdPedido = item.IdPedido,
                IdCliente = item.IdCliente,
                NomeCliente = item.NomeCliente,
                QuantidadePedida = item.QuantidadePedida,
                QuantidadeSugerida = sugerido,
                PossuiDividaVencida = false,
                ValorDividaVencida = 0m
            });
        }

        // Distribui para inadimplentes
        decimal entregueB = 0m;
        for (int idx = 0; idx < grupoInadimplente.Count; idx++)
        {
            var item = grupoInadimplente[idx];
            decimal sugerido;

            if (idx == grupoInadimplente.Count - 1)
            {
                sugerido = Math.Min(item.QuantidadePedida, estoqueParaInadimplentes - entregueB);
            }
            else
            {
                var fatia = totalInadimplente > 0 ? (item.QuantidadePedida / totalInadimplente) : 0;
                sugerido = Math.Min(item.QuantidadePedida, TruncarTresCasas(estoqueParaInadimplentes * fatia));
                entregueB += sugerido;
            }

            if (sugerido < 0) sugerido = 0;

            resultados.Add(new ItemCorteSugestaoOutput
            {
                IdItemPedido = item.IdItemPedido,
                IdPedido = item.IdPedido,
                IdCliente = item.IdCliente,
                NomeCliente = item.NomeCliente,
                QuantidadePedida = item.QuantidadePedida,
                QuantidadeSugerida = sugerido,
                PossuiDividaVencida = true,
                ValorDividaVencida = item.ValorDividaVencida
            });
        }

        return resultados;
    }

    private static decimal TruncarTresCasas(decimal quantidade) =>
        decimal.Floor(quantidade * 1000m) / 1000m;
}
