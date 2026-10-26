using SmartNetManager.Modelos;
using SmartNetManager.Servicos;

namespace SmartNetManager.Interface;

public static class DadosIniciais
{
    public static void Carregar(ServidorDhcp dhcp, GerenciadorRede rede)
    {
        // Reserva dos dois IPs fixo
        dhcp.Reservar("AA:BB:CC:11:22", "192.168.0.10");
        dhcp.Reservar("AA:BB:CC:33:44", "192.168.0.11");

        // Seis dispositivos aguardando autenticacao no inicio do programa
        var dispositivos = new[]
        {
            new Dispositivo { Nome = "PC-Contabilidade", MacAddress = "AA:BB:CC:11:22", Fabricante = "Dell",    ConsumoBanda = 45.5 },
            new Dispositivo { Nome = "iPhone-Diretoria", MacAddress = "DD:EE:FF:01:02", Fabricante = "Apple",   ConsumoBanda = 120.0 },
            new Dispositivo { Nome = "MacBook-Design",   MacAddress = "DD:EE:FF:03:04", Fabricante = "Apple",   ConsumoBanda = 310.0 },
            new Dispositivo { Nome = "Servidor-Backup",  MacAddress = "AA:BB:CC:33:44", Fabricante = "Dell",    ConsumoBanda = 650.0 }, // > 500: anomalia
            new Dispositivo { Nome = "Galaxy-Vendas",    MacAddress = "11:22:33:44:55", Fabricante = "Samsung", ConsumoBanda = 80.0 },
            new Dispositivo { Nome = "SmartTV-Recepcao", MacAddress = "66:77:88:99:AA", Fabricante = "Samsung", ConsumoBanda = 95.0 }
        };

        foreach (var dispositivo in dispositivos)
            rede.AdicionarNaFila(dispositivo);
    }
}
