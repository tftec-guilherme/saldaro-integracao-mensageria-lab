using Saldaro.Testes;

namespace Saldaro.Hub.Tests;

/// <summary>O gerador de CNPJ de teste usado pelos testes do hub e do banco simulado (contrato, seção 0).</summary>
public class CnpjDeTesteTests
{
    [Fact]
    public void Gera_cnpjs_diferentes_com_filial_0001_e_digitos_verificadores_validos()
    {
        var gerados = Enumerable.Range(0, 500).Select(_ => CnpjDeTeste.Gerar()).ToList();

        Assert.All(gerados, cnpj =>
        {
            Assert.Matches("^[0-9]{8}0001[0-9]{2}$", cnpj);
            Assert.NotEqual(1, cnpj[..8].Distinct().Count());
            Assert.True(DigitosVerificadoresConferem(cnpj), "os dígitos verificadores não conferem");
        });
        Assert.True(gerados.Distinct().Count() > 1, "o gerador devolveu sempre o mesmo número");
    }

    // Conferência independente do gerador: pesos de 2 a 9, da direita para a esquerda, recomeçando em 2.
    private static bool DigitosVerificadoresConferem(string cnpj)
    {
        int Digito(int quantidade)
        {
            var soma = 0;
            for (var i = 0; i < quantidade; i++)
            {
                soma += (cnpj[quantidade - 1 - i] - '0') * (2 + (i % 8));
            }

            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        return cnpj[12] - '0' == Digito(12) && cnpj[13] - '0' == Digito(13);
    }
}
