namespace Saldaro.BancoSimulado.Infraestrutura;

/// <summary>
/// Cliente de uma fila ou tabela que, no primeiro uso, garante que ela existe. A criação roda uma vez por processo,
/// mesmo com várias chamadas ao mesmo tempo, e só conta como feita depois de dar certo: se falhar, a próxima chamada
/// tenta de novo.
/// </summary>
/// <remarks>
/// O hub tem uma cópia desta classe, para as tabelas dele. A cópia é de propósito: cada app é publicado sozinho, e
/// um não depende do código do outro.
/// </remarks>
public sealed class CriadoNoPrimeiroUso<TCliente>(TCliente cliente, Func<TCliente, CancellationToken, Task> criar)
{
    private readonly SemaphoreSlim _criacao = new(1, 1);
    private volatile bool _criado;

    /// <summary>Devolve o cliente depois de garantir que o recurso existe.</summary>
    public async Task<TCliente> ObterAsync(CancellationToken cancellationToken)
    {
        if (_criado)
        {
            return cliente;
        }

        // Quem chega enquanto outra chamada cria espera aqui e, ao entrar, já encontra o recurso criado.
        await _criacao.WaitAsync(cancellationToken);
        try
        {
            if (!_criado)
            {
                await criar(cliente, cancellationToken);
                _criado = true;
            }
        }
        finally
        {
            _criacao.Release();
        }

        return cliente;
    }
}
