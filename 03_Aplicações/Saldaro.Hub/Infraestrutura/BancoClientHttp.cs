using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Infraestrutura;

/// <summary>Cliente HTTP do banco parceiro (typed client; timeout de 120 s configurado no registro).</summary>
public sealed class BancoClientHttp(HttpClient http, IOptions<HubOptions> hub) : IBancoClient
{
    public async Task<CobrancaEmitida> EmitirCobrancaAsync(
        SolicitacaoDeCobranca solicitacao, string? caos, CancellationToken cancellationToken)
    {
        if (http.BaseAddress is null)
        {
            throw new InvalidOperationException("O setting BancoBaseUrl não está configurado.");
        }

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, MontarRota(caos))
        {
            Content = JsonContent.Create(solicitacao, options: Json.Opcoes),
        };
        requisicao.Headers.Add(Cabecalhos.ApiKeyBanco, hub.Value.BancoApiKey);

        using var resposta = await http.SendAsync(requisicao, cancellationToken);
        if (resposta.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            throw new BancoIndisponivelException($"O banco respondeu 503 ao emitir a cobrança da fatura {solicitacao.FaturaId}.");
        }

        if (!resposta.IsSuccessStatusCode)
        {
            throw new BancoRespostaInesperadaException(
                $"O banco respondeu {(int)resposta.StatusCode} ao emitir a cobrança da fatura {solicitacao.FaturaId}.");
        }

        var cobranca = await resposta.Content.ReadFromJsonAsync<CobrancaEmitida>(Json.Opcoes, cancellationToken);
        if (cobranca is null || string.IsNullOrWhiteSpace(cobranca.IdCobranca))
        {
            throw new BancoRespostaInesperadaException($"O banco não devolveu idCobranca para a fatura {solicitacao.FaturaId}.");
        }

        return cobranca;
    }

    /// <summary>
    /// <c>cobrancas?{caos}</c>. Aceita <c>;</c> ou <c>&amp;</c> entre os botões e codifica cada nome e cada valor com
    /// <see cref="Uri.EscapeDataString(string)"/>: o campo vem do corpo da fatura e não pode virar outra URL nem ganhar
    /// outro parâmetro (<c>/</c>, <c>?</c>, <c>#</c>, <c>=</c> e <c>%</c> dentro de um valor viajam codificados), e o
    /// acento chega inteiro ao banco (<c>assinatura=inválida</c> vira <c>assinatura=inv%C3%A1lida</c>).
    /// </summary>
    public static string MontarRota(string? caos)
    {
        var botoes = (caos ?? string.Empty).Trim().TrimStart('?')
            .Split(['&', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Codificar);
        var consulta = string.Join('&', botoes);

        return consulta.Length == 0 ? "cobrancas" : $"cobrancas?{consulta}";

        // "nome=valor" ou só "nome" (ex.: embaralhar); o valor é tudo depois do primeiro "=", como o banco lê.
        static string Codificar(string botao)
        {
            var igual = botao.IndexOf('=');
            return igual < 0
                ? Uri.EscapeDataString(botao)
                : $"{Uri.EscapeDataString(botao[..igual].Trim())}={Uri.EscapeDataString(botao[(igual + 1)..].Trim())}";
        }
    }
}
