namespace Saldaro.Testes;

/// <summary>
/// CNPJ de teste gerado por algoritmo, com dígitos verificadores válidos (módulo 11), como pede o contrato do kit
/// (seção 0): nunca um CNPJ fixo ou conhecido. É o algoritmo do <c>New-CnpjTeste</c> dos scripts
/// <c>erp-simulado.ps1</c> e <c>smoke-test.ps1</c>: raiz aleatória de 8 dígitos (nunca todos iguais), filial
/// <c>0001</c> e os dois dígitos verificadores, sem pontuação.
/// </summary>
/// <remarks>
/// Um arquivo só para os testes do hub e do banco simulado: o <c>Saldaro.BancoSimulado.Tests.csproj</c> compila
/// este mesmo arquivo (<c>Compile Include</c> com <c>Link</c>), em vez de manter uma cópia.
/// </remarks>
internal static class CnpjDeTeste
{
    private static readonly int[] PesosDoPrimeiroDigito = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosDoSegundoDigito = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>Um CNPJ novo a cada chamada: 14 dígitos, sem pontuação.</summary>
    public static string Gerar()
    {
        var digitos = new int[14];
        do
        {
            for (var i = 0; i < 8; i++)
            {
                digitos[i] = Random.Shared.Next(0, 10);
            }
        }
        while (digitos.Take(8).Distinct().Count() == 1); // evita 00000000, 11111111...

        digitos[11] = 1; // filial 0001 (posições 8 a 11)
        digitos[12] = DigitoVerificador(digitos, PesosDoPrimeiroDigito);
        digitos[13] = DigitoVerificador(digitos, PesosDoSegundoDigito);
        return new string(Array.ConvertAll(digitos, digito => (char)('0' + digito)));
    }

    private static int DigitoVerificador(int[] digitos, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < pesos.Length; i++)
        {
            soma += digitos[i] * pesos[i];
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
