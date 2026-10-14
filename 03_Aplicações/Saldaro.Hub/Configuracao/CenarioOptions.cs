namespace Saldaro.Hub.Configuracao;

/// <summary>
/// Os toggles de cenário. Cada um vem de um app setting <c>Cenario__X</c> (o Bicep traduz os
/// parâmetros do <c>main.bicep</c> nesses settings) e é lido com o padrão Options como
/// <c>Cenario:X</c>. Tudo <c>false</c> = baseline ingênuo de propósito.
/// </summary>
public sealed class CenarioOptions
{
    public const string Secao = "Cenario";

    /// <summary>3.2: POST assíncrono (202 + status + fila) e <c>Idempotency-Key</c>.</summary>
    public bool AsyncRequestReply { get; set; }

    /// <summary>3.3: valida o HMAC do webhook do banco (401 se inválido).</summary>
    public bool WebhookHmac { get; set; }

    /// <summary>3.3: inbox + máquina de estados + ETag na baixa.</summary>
    public bool Inbox { get; set; }

    /// <summary>3.4: schema inválido vai para a DLQ na primeira entrega, com motivo.</summary>
    public bool DlqComMotivo { get; set; }

    /// <summary>3.4: falha transiente vira cópia agendada (5 s × 2^n, até 3 tentativas).</summary>
    public bool RetryAgendado { get; set; }

    /// <summary>3.6: a conciliação emite <c>conciliacao.heartbeat</c> ao concluir.</summary>
    public bool Heartbeat { get; set; }
}
