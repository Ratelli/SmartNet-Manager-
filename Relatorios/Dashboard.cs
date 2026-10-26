using SmartNetManager.Modelos;

namespace SmartNetManager.Relatorios;

// Painel estatistico com LINQ sobre os dispositivos online.
public static class Dashboard
{
    public const double LimiteAnomaliaMbps = 500;

    public static void Exibir(IEnumerable<Dispositivo> ativos)
    {
        ArgumentNullException.ThrowIfNull(ativos);

        // Materializa uma unica vez: as 4 consultas abaixo leem a mesma "foto" da rede.
        var online = ativos.ToList();

        Console.WriteLine("============== DASHBOARD DA REDE ==============");
        Console.WriteLine($"Dispositivos online: {online.Count}");
        Console.WriteLine();

        if (online.Count == 0)
        {
            Console.WriteLine("Nenhum dispositivo online no momento.");
            return;
        }

        // 1) Consumo total de banda -> Sum
        var consumoTotal = online.Sum(d => d.ConsumoBanda);
        Console.WriteLine($"1) Consumo total de banda: {consumoTotal:N1} Mbps");
        Console.WriteLine();

        // 2) Top 3 que mais consomem -> OrderByDescending + Take
        var top3 = online.OrderByDescending(d => d.ConsumoBanda).Take(3);

        Console.WriteLine("2) Top 3 maiores consumidores:");
        var posicao = 0;
        foreach (var dispositivo in top3)
            Console.WriteLine($"   {++posicao}. {dispositivo.Nome,-20} {dispositivo.ConsumoBanda,8:N1} Mbps");
        Console.WriteLine();

        // 3) Quantidade por fabricante -> GroupBy + Count
        //    OrdinalIgnoreCase: "apple" e "Apple" caem no mesmo grupo.
        var porFabricante = online
            .GroupBy(d => d.Fabricante, StringComparer.OrdinalIgnoreCase)
            .Select(grupo => new { Fabricante = grupo.Key, Quantidade = grupo.Count() })
            .OrderByDescending(item => item.Quantidade)
            .ThenBy(item => item.Fabricante);

        Console.WriteLine("3) Aparelhos por fabricante:");
        foreach (var item in porFabricante)
            Console.WriteLine($"   {item.Fabricante,-12} {item.Quantidade} aparelho(s)");
        Console.WriteLine();

        // 4) Alerta de anomalia -> Any (para no PRIMEIRO que satisfaz; nao percorre tudo)
        Console.WriteLine($"4) Verificacao de anomalia (acima de {LimiteAnomaliaMbps:N0} Mbps):");

        if (online.Any(d => d.ConsumoBanda > LimiteAnomaliaMbps))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("   *** ALERTA DE ANOMALIA ***");
            foreach (var suspeito in online.Where(d => d.ConsumoBanda > LimiteAnomaliaMbps))
                Console.WriteLine($"   {suspeito.Nome} ({suspeito.EnderecoIP}) consumindo {suspeito.ConsumoBanda:N1} Mbps");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("   Nenhuma anomalia detectada.");
        }
    }
}
