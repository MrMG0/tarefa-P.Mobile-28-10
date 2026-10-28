using VanderPan.Core.Enums;

namespace VanderPan.Core.Services;

public static class EntregaService
{
    /// <summary>
    /// Aloca a janela de entrega (12h ou 19h) mais próxima com base no horário do envio/pedido.
    /// Entregas são feitas às 12h e às 19h.
    /// </summary>
    public static JanelaEntrega AlocarJanela(DateTime horario)
    {
        var hora = horario.TimeOfDay.TotalHours;

        // Horários de entrega: 12.0 e 19.0.
        // Se enviado até 12:00 -> janela 12h
        // Se enviado entre 12:01 e 19:00 -> janela 19h
        // Se enviado após 19:00 -> janela 12h do próximo dia
        if (hora <= 12.0)
        {
            return JanelaEntrega.H12;
        }
        else if (hora <= 19.0)
        {
            return JanelaEntrega.H19;
        }
        else
        {
            return JanelaEntrega.H12;
        }
    }

    public static string FormatarJanela(JanelaEntrega janela)
    {
        return janela == JanelaEntrega.H12 ? "12h" : "19h";
    }
}
