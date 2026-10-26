namespace SmartNetManager.Servicos;

// MISSAO 4 - Historico de auditoria com rollback.
// Requisitos:
// Criar a classe HistoricoAuditoria com uma Stack<string>
// Registrar uma msg txt toda vez que uma regra for criada no firewall ou um dispositivo for autenticado.
// Opção de desfazer última ação utilizando o método pop().
public class HistoricoAuditoria
{
    private readonly Stack<string> _acoes = new();      // exigida pelo enunciado
    private readonly Stack<Action> _reversoes = new();  // como desfazer cada acao

    public int Quantidade => _acoes.Count;

    // Percorrer uma Stack com foreach entrega do topo para a base.
    public IEnumerable<string> Acoes => _acoes;

    public void Registrar(string descricao, Action reverter)
    {
        ArgumentNullException.ThrowIfNull(reverter);

        _acoes.Push($"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {descricao}");
        _reversoes.Push(reverter);
    }

    // Desfaz a ultima acao e devolve a mensagem do que foi revertido (ou null se vazia).
    // Primeiro EXECUTA a reversao e so depois faz o Pop(): se a reversao falhar,
    // nada e removido e as duas pilhas continuam sincronizadas.
    public string? DesfazerUltima()
    {
        if (_acoes.Count == 0) return null;

        _reversoes.Peek().Invoke();
        _reversoes.Pop();

        return _acoes.Pop();
    }
}
