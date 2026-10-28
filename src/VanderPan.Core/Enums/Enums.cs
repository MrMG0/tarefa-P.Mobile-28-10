namespace VanderPan.Core.Enums;

public enum UnidadeMedida
{
    UNIDADE,
    LITRO,
    KG,
    BALDE
}

public enum StatusPedido
{
    PENDENTE,
    CORTADO,
    CONFIRMADO,
    EM_SEPARACAO,
    ENVIADO,
    ENTREGUE,
    CANCELADO
}

public enum JanelaEntrega
{
    H12, // "12H"
    H19  // "19H"
}

public enum FormaPagamento
{
    BOLETO,
    PIX,
    DINHEIRO,
    CHEQUE,
    TRANSFERENCIA
}

public enum StatusContaReceber
{
    ABERTO,
    PAGO,
    VENCIDO,
    CANCELADO
}
