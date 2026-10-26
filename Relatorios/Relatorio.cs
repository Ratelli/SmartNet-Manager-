using SmartNetManager.Modelos;

namespace SmartNetManager.Relatorios;

// Gerador de relatorios universal.
// Requisitos:
// Criar um método ImprimirRelatorio utilizando o tipo genérico IEnumerable<Dispositivo>.
// O método deve receber tanto a fila de autenticação quanto a fila de dispositivos ativos e
// deve imprimir em tela usando um foreach.
public static class Relatorio
{
    public static void ImprimirRelatorio(IEnumerable<Dispositivo> colecao)
    {
        ArgumentNullException.ThrowIfNull(colecao);

        Console.WriteLine($"{"#",-3} {"Nome",-20} {"MAC",-17} {"IP",-15} {"Fabricante",-10} {"Banda",12}  {"Conexao",-19}");
        Console.WriteLine(new string('-', 104));

        var total = 0;

        foreach (var dispositivo in colecao)
        {
            total++;

            var ip = string.IsNullOrEmpty(dispositivo.EnderecoIP) ? "(pendente)" : dispositivo.EnderecoIP;
            var conexao = dispositivo.DataConexao == default
                ? "(aguardando)"
                : dispositivo.DataConexao.ToString("dd/MM/yyyy HH:mm:ss");

            Console.WriteLine(
                $"{total,-3} {Cortar(dispositivo.Nome, 20),-20} {dispositivo.MacAddress,-17} {ip,-15} " +
                $"{Cortar(dispositivo.Fabricante, 10),-10} {dispositivo.ConsumoBanda,7:N1} Mbps  {conexao,-19}");
        }

        if (total == 0)
            Console.WriteLine("(colecao vazia)");

        Console.WriteLine(new string('-', 104));
        Console.WriteLine($"Total: {total} dispositivo(s)");
    }

    // Evita que um nome muito longo desalinhe as colunas.
    private static string Cortar(string texto, int tamanho) =>
        texto.Length <= tamanho ? texto : texto[..(tamanho - 1)] + "~";
}
