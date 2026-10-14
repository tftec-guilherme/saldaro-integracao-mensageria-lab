using Saldaro.BancoSimulado.Dominio;

namespace Saldaro.BancoSimulado.Servicos;

/// <summary>Tabela <c>cobrancas</c>. Em produção, Table Storage; nos testes, uma lista em memória.</summary>
public interface IRepositorioDeCobrancas
{
    Task GravarAsync(Cobranca cobranca, CancellationToken cancellationToken);

    Task<IReadOnlyList<Cobranca>> ListarPorFaturaAsync(string faturaId, CancellationToken cancellationToken);
}

/// <summary>Fila <c>banco-pagamentos</c>, com entrega atrasada.</summary>
public interface IFilaDePagamentos
{
    Task EnfileirarAsync(string mensagem, TimeSpan atraso, CancellationToken cancellationToken);
}

/// <summary>A espera do botão <c>latencia</c>, separada para os testes não dormirem de verdade.</summary>
public interface IEspera
{
    Task EsperarAsync(TimeSpan duracao, CancellationToken cancellationToken);
}

public sealed class EsperaReal : IEspera
{
    public Task EsperarAsync(TimeSpan duracao, CancellationToken cancellationToken) =>
        duracao <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(duracao, cancellationToken);
}
