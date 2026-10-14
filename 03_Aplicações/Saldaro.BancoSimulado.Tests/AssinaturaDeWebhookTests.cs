using System.Text.RegularExpressions;
using Saldaro.BancoSimulado.Dominio;

namespace Saldaro.BancoSimulado.Tests;

/// <summary>
/// Vetor de teste compartilhado com o hub (o projeto Saldaro.Hub.Tests afirma o mesmo valor).
/// Se um dos lados mudar a regra de assinatura, os dois testes quebram juntos.
/// </summary>
internal static class VetorCompartilhado
{
    public const string Segredo = "segredo-de-teste-saldaro";
    public const long Timestamp = 1760000000;
    public const string Corpo = "{\"idEvento\":\"11111111-1111-1111-1111-111111111111\",\"tipo\":\"cobranca.paga\",\"faturaId\":\"22222222-2222-2222-2222-222222222222\",\"idCobranca\":\"COB-0a1b2c3d\",\"valor\":1250.00,\"ocorridoEm\":\"2026-11-10T09:30:00Z\",\"simulaTransiente\":false,\"transienteAte\":null}";
    public const string AssinaturaEsperada = "sha256=a34d32f6a9230ca8a5e9dfc0bd8241bc95ff7c3aabc38f6efc032dffa40816a9";
}

public class AssinaturaDeWebhookTests
{
    [Fact]
    public void Calcular_com_o_vetor_compartilhado_devolve_a_assinatura_esperada()
    {
        var assinatura = AssinaturaDeWebhook.Calcular(VetorCompartilhado.Segredo, VetorCompartilhado.Timestamp, VetorCompartilhado.Corpo);

        Assert.Equal(VetorCompartilhado.AssinaturaEsperada, assinatura);
    }

    [Fact]
    public void Calcular_devolve_o_prefixo_sha256_e_hex_minusculo_de_64_digitos()
    {
        var assinatura = AssinaturaDeWebhook.Calcular("outro-segredo", 1, "{}");

        Assert.Matches(new Regex("^sha256=[0-9a-f]{64}$"), assinatura);
    }

    [Theory]
    [InlineData("{\"idEvento\":\"11111111-1111-1111-1111-111111111111\",\"tipo\":\"cobranca.paga\",\"faturaId\":\"22222222-2222-2222-2222-222222222222\",\"idCobranca\":\"COB-0a1b2c3d\",\"valor\":1250.0,\"ocorridoEm\":\"2026-11-10T09:30:00Z\",\"simulaTransiente\":false,\"transienteAte\":null}")]
    [InlineData("{\"idEvento\": \"11111111-1111-1111-1111-111111111111\",\"tipo\":\"cobranca.paga\",\"faturaId\":\"22222222-2222-2222-2222-222222222222\",\"idCobranca\":\"COB-0a1b2c3d\",\"valor\":1250.00,\"ocorridoEm\":\"2026-11-10T09:30:00Z\",\"simulaTransiente\":false,\"transienteAte\":null}")]
    public void Calcular_com_qualquer_diferenca_no_corpo_muda_a_assinatura(string corpoAlterado)
    {
        // Mesmo JSON "equivalente" (1250.0 no lugar de 1250.00, um espaço a mais): outros bytes, outra assinatura.
        var assinatura = AssinaturaDeWebhook.Calcular(VetorCompartilhado.Segredo, VetorCompartilhado.Timestamp, corpoAlterado);

        Assert.NotEqual(VetorCompartilhado.AssinaturaEsperada, assinatura);
    }

    [Fact]
    public void Calcular_com_outro_timestamp_muda_a_assinatura()
    {
        var assinatura = AssinaturaDeWebhook.Calcular(VetorCompartilhado.Segredo, VetorCompartilhado.Timestamp + 1, VetorCompartilhado.Corpo);

        Assert.NotEqual(VetorCompartilhado.AssinaturaEsperada, assinatura);
    }

    [Fact]
    public void Corromper_mantem_o_formato_mas_troca_todos_os_digitos()
    {
        var corrompida = AssinaturaDeWebhook.Corromper(VetorCompartilhado.AssinaturaEsperada);

        Assert.Matches(new Regex("^sha256=[0-9a-f]{64}$"), corrompida);
        Assert.NotEqual(VetorCompartilhado.AssinaturaEsperada, corrompida);
        var original = VetorCompartilhado.AssinaturaEsperada[AssinaturaDeWebhook.Prefixo.Length..];
        var trocada = corrompida[AssinaturaDeWebhook.Prefixo.Length..];
        Assert.All(Enumerable.Range(0, original.Length), i => Assert.NotEqual(original[i], trocada[i]));
    }

    [Fact]
    public void Corromper_usa_o_complemento_de_cada_digito()
    {
        Assert.Equal("sha256=fedcba9876543210", AssinaturaDeWebhook.Corromper("sha256=0123456789abcdef"));
    }

    [Fact]
    public void Corromper_duas_vezes_volta_ao_original()
    {
        var duasVezes = AssinaturaDeWebhook.Corromper(AssinaturaDeWebhook.Corromper(VetorCompartilhado.AssinaturaEsperada));

        Assert.Equal(VetorCompartilhado.AssinaturaEsperada, duasVezes);
    }
}
