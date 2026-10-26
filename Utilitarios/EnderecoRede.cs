using System.Text.RegularExpressions;

namespace SmartNetManager.Utilitarios;

// Validacao e padronizacao de enderecos MAC e IP.
public static class EnderecoRede
{
    // Aceita 5 grupos (como formato do exemplo do enunciado: "AA:BB:CC:11:22")
    // ou 6 grupos (formato real de MAC: "AA:BB:CC:11:22:33").
    private static readonly Regex PadraoMac =
        new(@"^([0-9A-F]{2}:){4,5}[0-9A-F]{2}$", RegexOptions.Compiled);

    // Deixa o MAC sempre no mesmo formato: sem espacos, maiusculo e com ":".
    public static string NormalizarMac(string mac) =>
        (mac ?? string.Empty).Trim().ToUpperInvariant().Replace('-', ':');

    public static bool MacValido(string mac) => PadraoMac.IsMatch(NormalizarMac(mac));

    // IPv4 simples: 4 partes numericas entre 0 e 255.
    public static bool IpValido(string ip)
    {
        var partes = (ip ?? string.Empty).Trim().Split('.');

        return partes.Length == 4
            && partes.All(parte => parte.Length is >= 1 and <= 3
                                   && parte.All(char.IsDigit)
                                   && int.Parse(parte) <= 255);
    }
}
