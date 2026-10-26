using SmartNetManager.Modelos;
using SmartNetManager.Utilitarios;

namespace SmartNetManager.Servicos;

// Recebe o ServidorDhcp e o HistoricoAuditoria pelo construtor (injecao de dependencia): a classe usa esses servicos,
// mas nao é responsavel por cria-los.
public class GerenciadorRede
{
    // Faixa de IPs dinamicos, usado para quem não tem reserva de IP (192.168.0.100 a 192.168.0.254).
    private const string PrefixoFaixaDinamica = "192.168.0.";
    private const int InicioFaixaDinamica = 100;
    private const int FimFaixaDinamica = 254;

    // Como solicitado a fila de autenticação no formato FIFO.
    private readonly Queue<Dispositivo> _filaAutenticacao = new();

    // Lista de quem esta online; permite percorrer, ordenar, filtrar e remover.
    private readonly List<Dispositivo> _ativos = new();

    private readonly ServidorDhcp _dhcp;
    private readonly HistoricoAuditoria _historico;

    public GerenciadorRede(ServidorDhcp dhcp, HistoricoAuditoria historico)
    {
        _dhcp = dhcp ?? throw new ArgumentNullException(nameof(dhcp));
        _historico = historico ?? throw new ArgumentNullException(nameof(historico));
    }

    public IReadOnlyCollection<Dispositivo> FilaAutenticacao => _filaAutenticacao;
    public IReadOnlyList<Dispositivo> Ativos => _ativos;

    // Fila de autenticacao

    public ResultadoOperacao AdicionarNaFila(Dispositivo dispositivo)
    {
        ArgumentNullException.ThrowIfNull(dispositivo);

        dispositivo.MacAddress = EnderecoRede.NormalizarMac(dispositivo.MacAddress);

        if (string.IsNullOrWhiteSpace(dispositivo.Nome))
            return ResultadoOperacao.Falha("O nome do dispositivo e obrigatorio.");

        if (!EnderecoRede.MacValido(dispositivo.MacAddress))
            return ResultadoOperacao.Falha($"MAC invalido: '{dispositivo.MacAddress}'.");

        if (dispositivo.ConsumoBanda < 0)
            return ResultadoOperacao.Falha("O consumo de banda nao pode ser negativo.");

        // Um mesmo aparelho nao pode estar na fila e online ao mesmo tempo.
        var macEmUso = _filaAutenticacao.Concat(_ativos)
                                        .Any(d => d.MacAddress == dispositivo.MacAddress);
        if (macEmUso)
            return ResultadoOperacao.Falha($"O MAC {dispositivo.MacAddress} ja esta na fila ou online.");

        dispositivo.EnderecoIP = string.Empty;  // IP so e entregue apos autenticar
        dispositivo.DataConexao = default;

        _filaAutenticacao.Enqueue(dispositivo);
        return ResultadoOperacao.Ok(
            $"{dispositivo.Nome} entrou na fila. Posicao: {_filaAutenticacao.Count}.");
    }

    // O dispositivo mais antigo da fila recebe IP e vai para a lista de ativos, conforme exigido.
    public ResultadoOperacao AutenticarProximo()
    {
        // TryPeek: olha quem e o proximo SEM remover. So removemos se houver IP para ele.
        if (!_filaAutenticacao.TryPeek(out var proximo))
            return ResultadoOperacao.Falha("A fila de autenticacao esta vazia.");

        var ipReservado = _dhcp.ObterIpReservado(proximo.MacAddress);
        var ip = ipReservado ?? GerarIpDinamico();

        if (ip is null)
            return ResultadoOperacao.Falha(
                "Nao ha IP livre na faixa dinamica. O dispositivo continua na fila. Remova algum dispositivo ou contate o administrador para liberar mais faixa dinamica.");

        var dispositivo = _filaAutenticacao.Dequeue();
        dispositivo.EnderecoIP = ip;
        dispositivo.DataConexao = DateTime.Now;
        _ativos.Add(dispositivo);

        var origem = ipReservado is null ? "faixa dinamica" : "reserva DHCP";

        // Registra a acao e COMO desfaze-la.
        _historico.Registrar(
            $"AUTENTICACAO: {dispositivo.Nome} ({dispositivo.MacAddress}) entrou na rede com IP {ip} ({origem}).",
            () =>
            {
                _ativos.Remove(dispositivo);
                dispositivo.EnderecoIP = string.Empty;
                dispositivo.DataConexao = default;
                DevolverAoInicioDaFila(dispositivo);
            });

        return ResultadoOperacao.Ok(
            $"{dispositivo.Nome} autenticado com IP {ip} ({origem}). Restam {_filaAutenticacao.Count} na fila.");
    }

    // Firewall dinamico

    public IReadOnlyList<Dispositivo> AplicarFirewall(Func<Dispositivo, bool> regraBloqueio) =>
        AplicarFirewall(regraBloqueio, "regra personalizada");

    // Sobrecarga com uma descricao legivel da regra, usada no historico.
    public IReadOnlyList<Dispositivo> AplicarFirewall(Func<Dispositivo, bool> regraBloqueio, string descricaoRegra)
    {
        ArgumentNullException.ThrowIfNull(regraBloqueio);

        var bloqueados = _ativos
            .Select((dispositivo, posicao) => (Dispositivo: dispositivo, Posicao: posicao))
            .Where(item => regraBloqueio(item.Dispositivo))
            .ToList();

        var conjuntoBloqueado = bloqueados.Select(item => item.Dispositivo).ToHashSet();
        _ativos.RemoveAll(conjuntoBloqueado.Contains);

        var nomes = bloqueados.Count == 0
            ? "nenhum dispositivo"
            : string.Join(", ", bloqueados.Select(item => item.Dispositivo.Nome));

        // Registra a regra aplicada e como reverte-la.
        _historico.Registrar(
            $"FIREWALL: regra '{descricaoRegra}' aplicada. {bloqueados.Count} bloqueado(s): {nomes}.",
            () =>
            {
                // Reinsere em ordem crescente de posicao.
                foreach (var item in bloqueados.OrderBy(item => item.Posicao))
                    _ativos.Insert(item.Posicao, item.Dispositivo);
            });

        return bloqueados.Select(item => item.Dispositivo).ToList();
    }

    // Auxiliares privados

    // Primeiro IP da faixa dinamica que nao esteja em uso nem reservado.
    private string? GerarIpDinamico()
    {
        var ipsEmUso = _ativos.Select(d => d.EnderecoIP).ToHashSet();

        return Enumerable.Range(InicioFaixaDinamica, FimFaixaDinamica - InicioFaixaDinamica + 1)
                         .Select(final => PrefixoFaixaDinamica + final)
                         .FirstOrDefault(ip => !ipsEmUso.Contains(ip) && !_dhcp.IpEstaReservado(ip));
    }

    // Queue nao permite inserir no inicio. Para o rollback devolver o dispositivo
    // ao seu lugar original (o primeiro), a fila e reconstruida.
    private void DevolverAoInicioDaFila(Dispositivo dispositivo)
    {
        var restantes = _filaAutenticacao.ToArray();

        _filaAutenticacao.Clear();
        _filaAutenticacao.Enqueue(dispositivo);

        foreach (var item in restantes)
            _filaAutenticacao.Enqueue(item);
    }
}
