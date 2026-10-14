namespace Saldaro.Hub.Dominio;

// Exceções que o hub lança de propósito para o Service Bus reentregar a mensagem.
// Os nomes aparecem na tabela "exceptions" do Application Insights (coluna type) e nos KQL.

/// <summary>Confirmação fora do schema no baseline (<c>Cenario:DlqComMotivo = false</c>).</summary>
public sealed class ConfirmacaoInvalidaException(string mensagem) : Exception(mensagem);

/// <summary>Falha transiente simulada pelo banco, sem retry agendado (<c>Cenario:RetryAgendado = false</c>).</summary>
public sealed class FalhaTransienteSimuladaException(string mensagem) : Exception(mensagem);

/// <summary>O banco respondeu 503 (botão de caos <c>falhaTransiente</c>).</summary>
public sealed class BancoIndisponivelException(string mensagem) : Exception(mensagem);

/// <summary>Resposta inesperada do banco (status diferente de 2xx e de 503).</summary>
public sealed class BancoRespostaInesperadaException(string mensagem) : Exception(mensagem);

/// <summary>A fatura mudou entre a leitura e a gravação em todas as tentativas (ETag).</summary>
public sealed class ConflitoDeConcorrenciaException(string mensagem) : Exception(mensagem);
