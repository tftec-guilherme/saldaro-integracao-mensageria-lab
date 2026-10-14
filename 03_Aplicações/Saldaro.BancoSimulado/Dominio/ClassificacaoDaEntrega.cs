namespace Saldaro.BancoSimulado.Dominio;

public enum ResultadoDaEntrega
{
    /// <summary>2xx: o hub aceitou o webhook.</summary>
    Entregue,

    /// <summary>400, 401 e demais 4xx (exceto 408 e 429): o hub rejeitou o evento; repetir não muda nada.</summary>
    Rejeitada,

    /// <summary>408, 429, 5xx ou outro código inesperado: vale tentar de novo.</summary>
    FalhaTransiente,
}

public static class ClassificacaoDaEntrega
{
    // CONCEITO: transiente × permanente. Um 401 por assinatura inválida é rejeição definitiva: repetir só
    // gera ruído. Já 408, 429 e 5xx são transientes: a mensagem volta para a fila e o Functions tenta de
    // novo (até maxDequeueCount do host.json; depois ela vai para a fila banco-pagamentos-poison).
    public static ResultadoDaEntrega Classificar(int statusCode) => statusCode switch
    {
        >= 200 and < 300 => ResultadoDaEntrega.Entregue,
        408 or 429 => ResultadoDaEntrega.FalhaTransiente,
        >= 400 and < 500 => ResultadoDaEntrega.Rejeitada,
        _ => ResultadoDaEntrega.FalhaTransiente,
    };
}
