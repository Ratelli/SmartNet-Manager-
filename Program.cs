using System.Globalization;
using System.Text;
using SmartNetManager.Interface;
using SmartNetManager.Servicos;

namespace SmartNetManager;

internal static class Program
{
    private static void Main()
    {
        // Acentos e formato brasileiro de numeros/datas (1.234,5 e dd/MM/yyyy).
        Console.OutputEncoding = Encoding.UTF8;
        var culturaBrasil = new CultureInfo("pt-BR");
        CultureInfo.CurrentCulture = culturaBrasil;
        CultureInfo.DefaultThreadCurrentCulture = culturaBrasil;

        // Montagem dos objetos (composicao): cada servico recebe pronto o que precisa.
        var dhcp = new ServidorDhcp();
        var historico = new HistoricoAuditoria();
        var rede = new GerenciadorRede(dhcp, historico);

        DadosIniciais.Carregar(dhcp, rede);

        new Menu(dhcp, rede, historico).Executar();
    }
}
