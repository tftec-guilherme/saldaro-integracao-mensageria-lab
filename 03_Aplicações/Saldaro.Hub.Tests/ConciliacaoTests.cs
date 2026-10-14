using Microsoft.Extensions.Logging.Abstractions;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Tests;

/// <summary>Conciliação: contagens, divergências e heartbeat ao concluir (cenário 3.6).</summary>
public class ConciliacaoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly RepositorioFaturasEmMemoria _faturas = new();
    private readonly RepositorioConciliacaoEmMemoria _conciliacao = new();
    private readonly TelemetriaEmMemoria _telemetria = new();

    public ConciliacaoTests()
    {
        Semear("f-1", StatusFatura.Recebida, Agora.AddMinutes(-1));
        Semear("f-2", StatusFatura.CobrancaEmitida, Agora.AddMinutes(-11)); // divergência: sem confirmação há 11 min
        Semear("f-3", StatusFatura.CobrancaEmitida, Agora.AddMinutes(-2));
        Semear("f-4", StatusFatura.Baixada, Agora.AddMinutes(-30));
        Semear("f-5", StatusFatura.Notificada, Agora.AddMinutes(-30));
    }

    private void Semear(string faturaId, string status, DateTimeOffset atualizadaEm) =>
        _faturas.SalvarAsync(new Fatura
        {
            FaturaId = faturaId,
            ClienteId = "cliente-a",
            Valor = 100m,
            Status = status,
            CriadaEm = atualizadaEm,
            AtualizadaEm = atualizadaEm,
        }, CancellationToken.None).Wait();

    private ServicoDeConciliacao Servico(bool heartbeat) => new(
        _faturas, _conciliacao, _telemetria, Configuracoes.Cenario(heartbeat: heartbeat), new RelogioControlado(Agora),
        NullLogger<ServicoDeConciliacao>.Instance);

    [Fact]
    public async Task Conta_por_status_e_marca_cobranca_emitida_ha_mais_de_10_minutos_como_divergencia()
    {
        var resumo = await Servico(heartbeat: false).ExecutarAsync("execucao-1", CancellationToken.None);

        Assert.Equal(5, resumo.FaturasVerificadas);
        Assert.Equal(1, resumo.Divergencias);
        Assert.Equal(1, resumo.ContagemPorStatus[StatusFatura.Recebida]);
        Assert.Equal(2, resumo.ContagemPorStatus[StatusFatura.CobrancaEmitida]);
        Assert.Equal(0, resumo.ContagemPorStatus[StatusFatura.Registrada]);
        Assert.Equal(1, resumo.ContagemPorStatus[StatusFatura.Baixada]);
        Assert.Equal(1, resumo.ContagemPorStatus[StatusFatura.Notificada]);
        Assert.Same(resumo, Assert.Single(_conciliacao.Resumos));
        Assert.Equal("execucao-1", resumo.IdExecucao);
    }

    [Fact]
    public async Task Com_o_toggle_ligado_emite_o_heartbeat_com_as_propriedades_do_contrato()
    {
        await Servico(heartbeat: true).ExecutarAsync("execucao-1", CancellationToken.None);

        var heartbeat = _telemetria.Unico(EventosDeTelemetria.ConciliacaoHeartbeat);
        Assert.Equal("5", heartbeat["faturasVerificadas"]);
        Assert.Equal("1", heartbeat["divergencias"]);
        Assert.True(long.TryParse(heartbeat["duracaoMs"], out _));
    }

    [Fact]
    public async Task Baseline_nao_emite_heartbeat()
    {
        await Servico(heartbeat: false).ExecutarAsync("execucao-1", CancellationToken.None);

        Assert.Equal(0, _telemetria.Contar(EventosDeTelemetria.ConciliacaoHeartbeat));
    }

    [Fact]
    public async Task Heartbeat_so_ao_concluir_falha_no_meio_nao_emite()
    {
        // Se o heartbeat saísse no início, ficaria "verde" mesmo com a conciliação quebrando.
        _conciliacao.Falhar = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Servico(heartbeat: true).ExecutarAsync("execucao-1", CancellationToken.None));

        Assert.Equal(0, _telemetria.Contar(EventosDeTelemetria.ConciliacaoHeartbeat));
    }
}
