using System.Globalization;
using SmartNetManager.Modelos;
using SmartNetManager.Relatorios;
using SmartNetManager.Servicos;

namespace SmartNetManager.Interface;

// Menu inicial interativo como exigido
public class Menu
{
    private readonly ServidorDhcp _dhcp;
    private readonly GerenciadorRede _rede;
    private readonly HistoricoAuditoria _historico;

    public Menu(ServidorDhcp dhcp, GerenciadorRede rede, HistoricoAuditoria historico)
    {
        _dhcp = dhcp;
        _rede = rede;
        _historico = historico;
    }

    public void Executar()
    {
        while (true)
        {
            ExibirOpcoes();
            var opcao = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (opcao) // Opções disponiveis no menu interativo.
            {
                case "1": ReservarIp(); break;
                case "2": ListarReservas(); break;
                case "3": AdicionarNaFila(); break;
                case "4": ProcessarFila(); break;
                case "5": Dashboard.Exibir(_rede.Ativos); break;
                case "6": ImprimirFila(); break;
                case "7": ImprimirAtivos(); break;
                case "8": MenuFirewall(); break;
                case "9": ExibirHistorico(); break;
                case "10": DesfazerUltimaAcao(); break;
                case "0":
                    Console.WriteLine("Encerrando o SmartNet Manager. Ate logo!");
                    return;
                default:
                    Erro("Opcao invalida.");
                    break;
            }

            Pausar();
        }
    }

    private void ExibirOpcoes()
    {
        if (!Console.IsOutputRedirected) Console.Clear();

        Console.WriteLine("===================================================");
        Console.WriteLine("         SMARTNET MANAGER - Painel do Roteador      ");
        Console.WriteLine("===================================================");
        Console.WriteLine($" Fila: {_rede.FilaAutenticacao.Count} | Online: {_rede.Ativos.Count} | " +
                          $"Reservas: {_dhcp.Quantidade} | Historico: {_historico.Quantidade}");
        Console.WriteLine("---------------------------------------------------");
        Console.WriteLine(" [DHCP]      1 - Reservar IP fixo");
        Console.WriteLine("             2 - Listar reservas");
        Console.WriteLine(" [FILA]      3 - Adicionar dispositivo a fila");
        Console.WriteLine("             4 - Processar fila (Autenticar)");
        Console.WriteLine(" [PAINEL]    5 - Exibir Dashboard");
        Console.WriteLine(" [RELATORIO] 6 - Relatorio da fila de autenticacao");
        Console.WriteLine("             7 - Relatorio dos dispositivos ativos");
        Console.WriteLine(" [FIREWALL]  8 - Aplicar regra de bloqueio");
        Console.WriteLine(" [AUDITORIA] 9 - Exibir historico");
        Console.WriteLine("            10 - Desfazer ultima acao");
        Console.WriteLine("             0 - Sair");
        Console.WriteLine("---------------------------------------------------");
        Console.Write("Escolha uma opcao: ");
    }

    private void ReservarIp()
    {
        Console.WriteLine("=== Reservar IP fixo (DHCP) ===");
        var mac = LerTexto("MAC Address (ex.: AA:BB:CC:11:22): ");
        var ip = LerTexto("Endereco IP (ex.: 192.168.0.10): ");

        Exibir(_dhcp.Reservar(mac, ip));
    }

    private void ListarReservas()
    {
        Console.WriteLine("=== Reservas DHCP (MAC -> IP) ===");

        if (_dhcp.Quantidade == 0)
        {
            Console.WriteLine("Nenhuma reserva cadastrada.");
            return;
        }

        // Percorrer um Dictionary entrega pares chave/valor (KeyValuePair).
        foreach (var reserva in _dhcp.Reservas.OrderBy(r => r.Value))
            Console.WriteLine($"  {reserva.Key,-17} -> {reserva.Value}");
    }

    private void AdicionarNaFila()
    {
        Console.WriteLine("=== Adicionar dispositivo a fila de autenticacao ===");

        var dispositivo = new Dispositivo
        {
            Nome = LerTexto("Nome (ex.: PC-Contabilidade): "),
            MacAddress = LerTexto("MAC Address (ex.: AA:BB:CC:11:22): "),
            Fabricante = LerTexto("Fabricante (ex.: Dell, Apple, Samsung): "),
            ConsumoBanda = LerDouble("Consumo de banda em Mbps (ex.: 120,5): ")
        };

        Exibir(_rede.AdicionarNaFila(dispositivo));
    }

    private void ProcessarFila()
    {
        Console.WriteLine("=== Processar fila (Autenticar) ===");
        Exibir(_rede.AutenticarProximo());
    }

    private void ImprimirFila()
    {
        Console.WriteLine("=== Relatorio: Fila de Autenticacao (ordem de atendimento) ===");
        Relatorio.ImprimirRelatorio(_rede.FilaAutenticacao);
    }

    private void ImprimirAtivos()
    {
        Console.WriteLine("=== Relatorio: Dispositivos Ativos ===");
        Relatorio.ImprimirRelatorio(_rede.Ativos);
    }

    private void MenuFirewall()
    {
        Console.WriteLine("=== Firewall Dinamico ===");

        if (_rede.Ativos.Count == 0)
        {
            Aviso("Nenhum dispositivo online para filtrar.");
            return;
        }

        Console.WriteLine(" 1 - Bloquear aparelhos da Apple");
        Console.WriteLine(" 2 - Bloquear aparelhos consumindo mais de 100 Mbps");
        Console.WriteLine(" 3 - Bloquear por fabricante (digitar)");
        Console.WriteLine(" 4 - Bloquear acima de um consumo (digitar)");
        Console.WriteLine(" 0 - Voltar");
        Console.Write("Escolha a regra: ");

        IReadOnlyList<Dispositivo>? bloqueados = Console.ReadLine()?.Trim() switch
        {
            "1" => _rede.AplicarFirewall(d => d.Fabricante == "Apple", "Fabricante == Apple"),
            "2" => _rede.AplicarFirewall(d => d.ConsumoBanda > 100, "Consumo > 100 Mbps"),
            "3" => BloquearPorFabricante(),
            "4" => BloquearPorConsumo(),
            _ => null
        };

        if (bloqueados is null) return;

        if (bloqueados.Count == 0)
        {
            Aviso("Regra aplicada, mas nenhum dispositivo se enquadrou.");
            return;
        }

        Sucesso($"{bloqueados.Count} dispositivo(s) bloqueado(s) e removido(s) da rede:");
        foreach (var dispositivo in bloqueados)
            Console.WriteLine($"  - {dispositivo.Nome} ({dispositivo.Fabricante}, {dispositivo.ConsumoBanda:N1} Mbps)");
    }

    private IReadOnlyList<Dispositivo> BloquearPorFabricante()
    {
        var fabricante = LerTexto("Fabricante a bloquear: ");

        return _rede.AplicarFirewall(
            d => string.Equals(d.Fabricante, fabricante, StringComparison.OrdinalIgnoreCase),
            $"Fabricante == {fabricante}");
    }

    private IReadOnlyList<Dispositivo> BloquearPorConsumo()
    {
        var limite = LerDouble("Bloquear acima de quantos Mbps? ");

        return _rede.AplicarFirewall(d => d.ConsumoBanda > limite, $"Consumo > {limite:N1} Mbps");
    }

    private void ExibirHistorico()
    {
        Console.WriteLine("=== Historico de Auditoria (mais recente primeiro) ===");

        if (_historico.Quantidade == 0)
        {
            Console.WriteLine("Nenhuma acao registrada.");
            return;
        }

        var numero = 0;
        foreach (var acao in _historico.Acoes)
            Console.WriteLine($"{++numero,2}. {acao}");
    }

    private void DesfazerUltimaAcao()
    {
        Console.WriteLine("=== Desfazer ultima acao ===");

        var revertida = _historico.DesfazerUltima();

        if (revertida is null)
        {
            Aviso("Nao ha acoes para desfazer.");
            return;
        }

        Sucesso("Acao revertida:");
        Console.WriteLine($"  {revertida}");
    }

    private static string LerTexto(string rotulo)
    {
        while (true)
        {
            Console.Write(rotulo);
            var texto = Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(texto)) return texto;

            Erro("Campo obrigatorio.");
        }
    }

    private static double LerDouble(string rotulo)
    {
        while (true)
        {
            var texto = LerTexto(rotulo).Replace('.', ',');

            if (double.TryParse(texto, NumberStyles.Float, new CultureInfo("pt-BR"), out var valor) && valor >= 0)
                return valor;

            Erro("Informe um numero valido e nao negativo.");
        }
    }

    private static void Exibir(ResultadoOperacao resultado)
    {
        if (resultado.Sucesso) Sucesso(resultado.Mensagem);
        else Erro(resultado.Mensagem);
    }

    private static void Sucesso(string mensagem) => Colorido(mensagem, ConsoleColor.Green);
    private static void Aviso(string mensagem) => Colorido(mensagem, ConsoleColor.Yellow);
    private static void Erro(string mensagem) => Colorido(mensagem, ConsoleColor.Red);

    private static void Colorido(string mensagem, ConsoleColor cor)
    {
        Console.ForegroundColor = cor;
        Console.WriteLine(mensagem);
        Console.ResetColor();
    }

    private static void Pausar()
    {
        Console.WriteLine();
        Console.Write("Pressione ENTER para continuar...");
        Console.ReadLine();
    }
}
