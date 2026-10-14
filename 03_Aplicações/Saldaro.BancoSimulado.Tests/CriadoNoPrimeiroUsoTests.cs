using Saldaro.BancoSimulado.Infraestrutura;

namespace Saldaro.BancoSimulado.Tests;

/// <summary>A criação da fila e da tabela no primeiro uso (<see cref="FilaDePagamentos"/> e <see cref="RepositorioDeCobrancas"/>).</summary>
public class CriadoNoPrimeiroUsoTests
{
    [Fact]
    public async Task Chamadas_simultaneas_criam_o_recurso_uma_vez_so()
    {
        var criacoes = 0;
        var liberar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cliente = new object();
        var recurso = new CriadoNoPrimeiroUso<object>(cliente, async (_, _) =>
        {
            Interlocked.Increment(ref criacoes);
            await liberar.Task;
        });

        // As dez chamadas chegam enquanto a primeira ainda está criando.
        var chamadas = Enumerable.Range(0, 10).Select(_ => recurso.ObterAsync(CancellationToken.None)).ToList();
        liberar.SetResult();
        var clientes = await Task.WhenAll(chamadas);

        Assert.Equal(1, criacoes);
        Assert.All(clientes, devolvido => Assert.Same(cliente, devolvido));
    }

    [Fact]
    public async Task Criacao_que_falhou_e_tentada_de_novo_na_proxima_chamada()
    {
        var tentativas = 0;
        var recurso = new CriadoNoPrimeiroUso<string>(FilaDePagamentos.NomeDaFila, (_, _) =>
            ++tentativas == 1
                ? Task.FromException(new InvalidOperationException("Azurite fora do ar (simulado)."))
                : Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(() => recurso.ObterAsync(CancellationToken.None));
        Assert.Equal(FilaDePagamentos.NomeDaFila, await recurso.ObterAsync(CancellationToken.None));
        Assert.Equal(FilaDePagamentos.NomeDaFila, await recurso.ObterAsync(CancellationToken.None));

        Assert.Equal(2, tentativas);
    }
}
