using SmartNetManager.Modelos;
using SmartNetManager.Utilitarios;

namespace SmartNetManager.Servicos;

public class ServidorDhcp
{
    private readonly Dictionary<string, string> _reservas = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> Reservas => _reservas;

    public int Quantidade => _reservas.Count;

    public ResultadoOperacao Reservar(string macAddress, string enderecoIp)
    {
        var mac = EnderecoRede.NormalizarMac(macAddress);
        var ip = (enderecoIp ?? string.Empty).Trim();

        // Se o MAC nao for valido, retorna erro.
        if (!EnderecoRede.MacValido(mac))
            return ResultadoOperacao.Falha($"MAC invalido: '{macAddress}'. Use o formato correto: AA:BB:CC:11:22.");

        // Se o IP nao for valido, retorna erro.
        if (!EnderecoRede.IpValido(ip))
            return ResultadoOperacao.Falha($"IP invalido: '{enderecoIp}'. Use o formato correto: 192.168.0.10.");

        // Bloqueia para que o mesmo MAC nao seja cadastrado duas vezes conforme exigido.
        if (_reservas.TryGetValue(mac, out var ipExistente))
            return ResultadoOperacao.Falha($"O MAC {mac} ja possui reserva para o IP {ipExistente}.");

        // Regra para reservar IP para um MAC
        if (_reservas.ContainsValue(ip))
            return ResultadoOperacao.Falha($"O IP {ip} ja esta reservado para outro dispositivo.");

        // Confirmação da reserva
        _reservas.Add(mac, ip);
        return ResultadoOperacao.Ok($"Reserva criada: {mac} -> {ip}");
    }

    // Retorna o IP reservado para o MAC informado, ou null se nao houver reserva.
    public string? ObterIpReservado(string macAddress) =>
        _reservas.TryGetValue(EnderecoRede.NormalizarMac(macAddress), out var ip) ? ip : null;

    public bool IpEstaReservado(string enderecoIp) => _reservas.ContainsValue(enderecoIp);
}
